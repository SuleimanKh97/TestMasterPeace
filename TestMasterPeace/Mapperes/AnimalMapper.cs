using TestMasterPeace.DTOs.AnimalsDTOs;
using TestMasterPeace.TestModeles;

namespace TestMasterPeace.Mappers;

public static class AnimalMapper
{
    internal static Animal ToAnimal(CreateAnimalRequest newAnimal)
    {
        var random = new Random();
        return new Animal
        {
            Description = newAnimal.Description,
            Name = newAnimal.Name,
            Id = random.Next()
        };
    }
}
