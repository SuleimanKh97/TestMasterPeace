using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
// using TestMasterPeace.Data; // Assuming DbContext is in Models namespace now
using TestMasterPeace.DTOs;
using TestMasterPeace.Models; // Use correct namespace for Models

namespace TestMasterPeace.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication for feedback
    public class FeedbackController : ControllerBase
    {
        // Use the correct DbContext name: MasterPeiceContext
        private readonly MasterPeiceContext _context;
        private readonly ILogger<FeedbackController> _logger;

        // Update constructor parameter type to MasterPeiceContext
        public FeedbackController(MasterPeiceContext context, ILogger<FeedbackController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogInformation("FeedbackController instance created. DbContext HashCode: {DbContextHashCode}", _context.GetHashCode());
        }

        // POST: api/feedback
        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] CreateFeedbackDto feedbackDto)
        {
            // Use a scope for logging request details
            using (_logger.BeginScope(new Dictionary<string, object> { ["RequestTraceId"] = HttpContext.TraceIdentifier }))
            {
                _logger.LogInformation("Processing feedback submission. DbContext HashCode: {DbContextHashCode}", _context.GetHashCode());
                _logger.LogInformation("Received feedback submission request for ProductId: {ProductId}, Rating: {Rating}, Comment: '{Comment}'",
                    feedbackDto.ProductId, feedbackDto.Rating, feedbackDto.Comment);

                // 1. Get User ID from Claims
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out long userId))
                {
                    _logger.LogWarning("Unauthorized attempt: User ID claim is missing or invalid.");
                    return Unauthorized(new { Message = "User ID claim is missing or invalid." });
                }
                _logger.LogInformation("User ID {UserId} identified for feedback.", userId);

                // --- Enhanced Product Existence Check ---
                long productIdToCheck = feedbackDto.ProductId;
                _logger.LogInformation("--- Start Product Existence Check for ProductId: {ProductId} ---", productIdToCheck);

                // A) Try AnyAsync (Original Check)
                bool productExistsAny = false;
                try
                {
                    _logger.LogInformation("Checking with AnyAsync for ProductId: {ProductId}", productIdToCheck);
                    productExistsAny = await _context.Products.AnyAsync(p => p.Id == productIdToCheck);
                    _logger.LogInformation("AnyAsync check result for ProductId {ProductId}: {Exists}", productIdToCheck, productExistsAny);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during AnyAsync check for ProductId {ProductId}", productIdToCheck);
                }

                // B) Try FirstOrDefaultAsync (Debug Check)
                Product? foundProduct = null;
                try
                {
                    _logger.LogInformation("Checking with FirstOrDefaultAsync for ProductId: {ProductId}", productIdToCheck);
                    foundProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == productIdToCheck);
                    _logger.LogInformation("FirstOrDefaultAsync check result for ProductId {ProductId}: Found Product = {FoundProduct}",
                        productIdToCheck, foundProduct != null ? $"Yes (ID: {foundProduct.Id})" : "No");
                    if (foundProduct != null) {
                         _logger.LogInformation("Found Product Name: {ProductName}", foundProduct.Name);
                    }
                }
                catch (Exception ex)
                {
                     _logger.LogError(ex, "Error during FirstOrDefaultAsync check for ProductId {ProductId}", productIdToCheck);
                }

                _logger.LogInformation("--- End Product Existence Check for ProductId: {ProductId} ---", productIdToCheck);

                // ** Use the result from AnyAsync for the actual logic **
                if (!productExistsAny)
                {
                    _logger.LogWarning("Product not found based on AnyAsync check for ProductId: {ProductId}. Returning 404.", productIdToCheck);
                    return NotFound(new { Message = $"Product with ID {productIdToCheck} not found." });
                }
                // --- End Enhanced Check ---

                // 3. **Crucial Check:** Verify the user actually ordered and received this product.
                _logger.LogInformation("Checking if User {UserId} ordered Product {ProductId}", userId, productIdToCheck);
                bool hasUserOrderedProduct = false;
                try
                {
                    hasUserOrderedProduct = await _context.OrderItems
                        .Include(oi => oi.Order) // Include Order to get User info
                        .AnyAsync(oi => oi.ProductId == productIdToCheck &&
                                         oi.Order.UserId == userId && // Check if order belongs to the user
                                         oi.Order.Status == "Delivered"); // Ensure order was delivered
                    _logger.LogInformation("User order check result for User {UserId}, Product {ProductId}: {Ordered}", userId, productIdToCheck, hasUserOrderedProduct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking user order for User {UserId}, Product {ProductId}", userId, productIdToCheck);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error checking user order.");
                }

                if (!hasUserOrderedProduct)
                {
                    _logger.LogWarning("Forbidden: User {UserId} did not order or receive Product {ProductId}", userId, productIdToCheck);
                    return Forbid(); // Or BadRequest - User didn't order/receive this specific product or order not delivered
                }

                // 4. Optional: Check if user already submitted feedback for this product (prevent duplicates?)
                _logger.LogInformation("Checking for existing feedback from User {UserId} for Product {ProductId}", userId, productIdToCheck);
                Feedback? existingFeedback = null;
                try
                {
                    existingFeedback = await _context.Feedbacks
                                           .FirstOrDefaultAsync(f => f.ProductId == productIdToCheck && f.UserId == userId);
                    _logger.LogInformation("Existing feedback check result for User {UserId}, Product {ProductId}: {Found}", userId, productIdToCheck, existingFeedback != null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking existing feedback for User {UserId}, Product {ProductId}", userId, productIdToCheck);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error checking existing feedback.");
                }

                if (existingFeedback != null)
                {
                    _logger.LogInformation("Updating existing feedback for User {UserId}, Product {ProductId}", userId, productIdToCheck);
                    existingFeedback.Rating = feedbackDto.Rating;
                    existingFeedback.Comment = feedbackDto.Comment;
                    existingFeedback.CreatedAt = DateTime.UtcNow; // Update timestamp
                    _context.Feedbacks.Update(existingFeedback);
                    await _context.SaveChangesAsync();
                    return Ok(new { Message = "Feedback updated successfully.", Feedback = existingFeedback });

                    // Option 2: Prevent duplicate feedback
                    // return BadRequest(new { Message = "You have already submitted feedback for this product." });
                }

                // 5. Create and Save New Feedback
                _logger.LogInformation("Creating new feedback for User {UserId}, Product {ProductId}", userId, productIdToCheck);
                var feedback = new Feedback
                {
                    UserId = userId,
                    ProductId = productIdToCheck,
                    Rating = feedbackDto.Rating,
                    Comment = feedbackDto.Comment,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created new feedback with ID {FeedbackId}", feedback.Id);
                return Ok(new { Message = "Feedback submitted successfully.", FeedbackId = feedback.Id });
            } // End logging scope
        }

        // Optional: Add a GET method if needed later
        // [HttpGet("{id}")]
        // public async Task<ActionResult<Feedback>> GetFeedback(long id)
        // {
        //     var feedback = await _context.Feedbacks.FindAsync(id);
        //     if (feedback == null)
        //     {
        //         return NotFound();
        //     }
        //     // Add authorization check if needed (e.g., only user or admin can view)
        //     return feedback;
        // }
    }
} 