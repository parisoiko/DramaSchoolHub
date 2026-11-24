using DramaSchoolHub.Models;

namespace DramaSchoolHub.Services;

// The school's rehearsal rooms, kept in memory: the project is about DI and middleware, not data access.
public class RoomCatalogue : IRoomCatalogue
{
    private readonly List<RehearsalRoom> _rooms =
    [
        new RehearsalRoom { Id = 1, Name = "Main Stage", Capacity = 120, HasPiano = true },
        new RehearsalRoom { Id = 2, Name = "Studio A", Capacity = 25, HasPiano = true },
        new RehearsalRoom { Id = 3, Name = "Studio B", Capacity = 15, HasPiano = false },
        new RehearsalRoom { Id = 4, Name = "Black Box", Capacity = 40, HasPiano = false },
    ];

    public List<RehearsalRoom> GetAll() => _rooms;

    public RehearsalRoom? GetById(int id) => _rooms.FirstOrDefault(room => room.Id == id);
}
