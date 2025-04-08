using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestMasterPeace.DTOs.AnimalsDTOs;
using TestMasterPeace.Services;

namespace TestMasterPeace.Controllers;

[ApiController]
[Route("[controller]")]
public class AnimalsController(AnimalsService animalsService) : ControllerBase
{

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get()
    {
        return Ok(animalsService.GetAnimals());
    }

    [HttpGet("{id:int}")]
    public IActionResult GetAnimalById(int id)
    {
        var animal = animalsService.GetAnimalById(id);
        if (animal == null) return NotFound();
        return Ok(animal);
    }
    [HttpPost]
    public async Task<IActionResult> Post(CreateAnimalRequest newAnimal)
    {
        _ = animalsService.AddAnimal(newAnimal);
        return Ok();
    }
}
