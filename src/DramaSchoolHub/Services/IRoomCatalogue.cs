using DramaSchoolHub.Models;

namespace DramaSchoolHub.Services;

public interface IRoomCatalogue
{
    List<RehearsalRoom> GetAll();
    RehearsalRoom? GetById(int id);
}