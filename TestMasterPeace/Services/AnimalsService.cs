using System.Collections.Generic;
using TestMasterPeace.DTOs.AnimalsDTOs;
using TestMasterPeace.Mappers;
using TestMasterPeace.TestModeles;

namespace TestMasterPeace.Services;

public class AnimalsService
{
    private List<Animal> animals = [
    new Animal
    {
        Id = 1,
        Name = "Lion",
        Description = "A large carnivorous feline mammal of Africa and northwest India, with a short tawny coat, a tufted tail, and, in the male, a heavy mane around the neck and shoulders."
    },
    new Animal
    {
        Id = 2,
        Name = "Elephant",
        Description = "A heavy plant-eating mammal with a prehensile trunk, long curved ivory tusks, and large ears, native to Africa and southern Asia."
    },
    new Animal
    {
        Id = 3,
        Name = "Dolphin",
        Description = "A small gregarious toothed whale that typically has a beaklike snout and a curved fin on the back."
    },
    new Animal
    {
        Id = 4,
        Name = "Eagle",
        Description = "A large bird of prey with a massive hooked bill and long broad wings, known for its keen sight and powerful soaring flight."
    },
    new Animal
    {
        Id = 5,
        Name = "Kangaroo",
        Description = "A herbivorous marsupial with a long powerful tail and strongly developed hind limbs that enable it to travel by leaping, native to Australia."
    },
    new Animal
    {
        Id = 6,
        Name = "Penguin",
        Description = "A flightless seabird of the southern hemisphere, with black upper parts and white underparts and wings developed into flippers for swimming under water."
    },
    new Animal
    {
        Id = 7,
        Name = "Giraffe",
        Description = "A large African mammal with a very long neck and forelegs, having a coat patterned with brown patches separated by lighter lines."
    },
    new Animal
    {
        Id = 8,
        Name = "Tiger",
        Description = "A very large solitary cat with a yellow-brown coat striped with black, native to the forests of Asia."
    },
    new Animal
    {
        Id = 9,
        Name = "Octopus",
        Description = "A cephalopod mollusk with eight sucker-bearing arms, a soft sac-like body, strong beak-like jaws, and no internal shell."
    },
    new Animal
    {
        Id = 10,
        Name = "Butterfly",
        Description = "A nectar-feeding insect with two pairs of large, typically brightly colored wings that are covered with microscopic scales."
    }
];
    public List<Animal> GetAnimals()
    {
        return animals;
    }
    public int AddAnimal(CreateAnimalRequest newAnimalRequest)
    {
        var newAnimal = AnimalMapper.ToAnimal(newAnimalRequest);
        animals.Add(newAnimal);
        return newAnimal.Id;
    }
    public bool DeleteAnimal(int id)
    {
        var deletedAnimal = animals.Find(animal => animal.Id == id);
        if (deletedAnimal == null)
            return false;

        animals.Remove(deletedAnimal);
        return true;
    }
    public bool UpdateAnimal(int id, Animal updatedAnimal)
    {
        var oldAnimal = animals.Find(animal => animal.Id == id);
        var deleted = DeleteAnimal(id);
        if (!deleted) return false;
        animals.Add(updatedAnimal);
        return true;
    }

    public Animal? GetAnimalById(int id)
    {
        return animals.Find(animal => animal.Id == id);
    }
}
