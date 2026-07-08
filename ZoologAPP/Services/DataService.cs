using System.Text.Json;
using ZoologAPP.Models;

namespace ZoologAPP.Services;

public class DataService
{
    const string ColKey = "collections_db";
    const string ObjKey = "objects_db";
    const string LoanKey = "loans_db";
    const string LocationKey = "locations_db";

    // --- Sammlungen ---

    public List<Collection> GetCollections()
    {
        var json = Preferences.Get(ColKey, "[]");
        return JsonSerializer.Deserialize<List<Collection>>(json) ?? [];
    }

    void SaveCollections(List<Collection> list) =>
        Preferences.Set(ColKey, JsonSerializer.Serialize(list));

    public Collection CreateCollection(string name, string desc, bool isPublic, string ownerId, string ownerName)
    {
        var c = new Collection
        {
            Name = name.Trim(),
            Description = desc.Trim(),
            IsPublic = isPublic,
            OwnerId = ownerId,
            OwnerName = ownerName
        };
        var list = GetCollections();
        list.Add(c);
        SaveCollections(list);
        return c;
    }

    public void DeleteCollection(string id)
    {
        var list = GetCollections();
        list.RemoveAll(c => c.Id == id);
        SaveCollections(list);

        // objekte der sammlung auch löschen
        var objs = GetObjects();
        objs.RemoveAll(o => o.CollectionId == id);
        SaveObjects(objs);
    }

    public List<Collection> GetMyCollections(string userId) =>
        GetCollections().Where(c => c.OwnerId == userId).ToList();

    public List<Collection> GetPopularCollections() =>
        GetCollections()
            .Where(c => c.IsPublic)
            .OrderByDescending(c => GetObjectCountForCollection(c.Id))
            .Take(10)
            .ToList();

    // --- Favoriten ---

    static string FavKey(string userId) => $"favs_{userId}";

    public List<string> GetFavoriteIds(string userId)
    {
        var json = Preferences.Get(FavKey(userId), "[]");
        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }

    public bool IsFavorite(string userId, string colId) =>
        GetFavoriteIds(userId).Contains(colId);

    public void ToggleFavorite(string userId, string colId)
    {
        var favs = GetFavoriteIds(userId);
        if (!favs.Remove(colId))
            favs.Add(colId);
        Preferences.Set(FavKey(userId), JsonSerializer.Serialize(favs));
    }

    public List<Collection> GetFavorites(string userId)
    {
        var ids = GetFavoriteIds(userId);
        return GetCollections().Where(c => ids.Contains(c.Id)).ToList();
    }

    // --- Objekte ---

    public List<ZoologObject> GetObjects()
    {
        var json = Preferences.Get(ObjKey, "[]");
        return JsonSerializer.Deserialize<List<ZoologObject>>(json) ?? [];
    }

    void SaveObjects(List<ZoologObject> list) =>
        Preferences.Set(ObjKey, JsonSerializer.Serialize(list));

    public ZoologObject CreateObject(ZoologObject obj)
    {
        var list = GetObjects();
        list.Add(obj);
        SaveObjects(list);
        return obj;
    }

    public void DeleteObject(string id)
    {
        var list = GetObjects();
        list.RemoveAll(o => o.Id == id);
        SaveObjects(list);
    }

    //08.07.2026 Alexander Stojek: Aktualisiert ein vorhandenes Exponat (für die Bearbeiten-Funktion in der App).
    public void UpdateObject(ZoologObject updated)
    {
        var list = GetObjects();
        var index = list.FindIndex(o => o.Id == updated.Id);
        if (index < 0)
            return;

        list[index] = updated;
        SaveObjects(list);
    }

    public List<ZoologObject> GetObjectsInCollection(string colId) =>
        GetObjects().Where(o => o.CollectionId == colId).ToList();

    public int GetObjectCountForCollection(string colId) =>
        GetObjects().Count(o => o.CollectionId == colId);

    public int GetBorrowedCount(string userId)
    {
        var myColIds = GetMyCollections(userId).Select(c => c.Id).ToHashSet();
        return GetObjects().Count(o => myColIds.Contains(o.CollectionId) && o.Status == "ausgeliehen");
    }

    // --- Leihgaben ---

    public List<Loan> GetLoans()
    {
        var json = Preferences.Get(LoanKey, "[]");
        return JsonSerializer.Deserialize<List<Loan>>(json) ?? [];
    }

    void SaveLoans(List<Loan> list) =>
        Preferences.Set(LoanKey, JsonSerializer.Serialize(list));

    public Loan CreateLoan(string objectId, string borrowerName, DateTime? dueAt, string notes, string createdBy)
    {
        var loan = new Loan
        {
            ObjectId = objectId,
            BorrowerName = borrowerName.Trim(),
            DueAt = dueAt,
            Notes = notes.Trim(),
            CreatedBy = createdBy
        };

        var loans = GetLoans();
        loans.Add(loan);
        SaveLoans(loans);

        UpdateObjectStatus(objectId, "ausgeliehen");
        return loan;
    }

    public void ReturnLoan(string id)
    {
        var loans = GetLoans();
        var loan = loans.FirstOrDefault(l => l.Id == id);
        if (loan is null)
            return;

        loan.ReturnedAt = DateTime.UtcNow;
        SaveLoans(loans);
        UpdateObjectStatus(loan.ObjectId, "verfügbar");
    }

    public List<Loan> GetOpenLoans() =>
        GetLoans().Where(l => l.IsOpen).OrderBy(l => l.DueAt ?? DateTime.MaxValue).ToList();

    public void DeleteLoan(string id)
    {
        var loans = GetLoans();
        loans.RemoveAll(l => l.Id == id);
        SaveLoans(loans);
    }

    void UpdateObjectStatus(string objectId, string status)
    {
        var objects = GetObjects();
        var obj = objects.FirstOrDefault(o => o.Id == objectId);
        if (obj is null)
            return;

        obj.Status = status;
        SaveObjects(objects);
    }

    // --- Standorte ---

    public List<StorageLocation> GetLocations()
    {
        var json = Preferences.Get(LocationKey, "[]");
        return JsonSerializer.Deserialize<List<StorageLocation>>(json) ?? [];
    }

    void SaveLocations(List<StorageLocation> list) =>
        Preferences.Set(LocationKey, JsonSerializer.Serialize(list));

    public StorageLocation CreateLocation(string name, string building, string room, string shelf, string notes)
    {
        var location = new StorageLocation
        {
            Name = name.Trim(),
            Building = building.Trim(),
            Room = room.Trim(),
            Shelf = shelf.Trim(),
            Notes = notes.Trim()
        };

        var locations = GetLocations();
        locations.Add(location);
        SaveLocations(locations);
        return location;
    }

    public void DeleteLocation(string id)
    {
        var locations = GetLocations();
        locations.RemoveAll(l => l.Id == id);
        SaveLocations(locations);

        var objects = GetObjects();
        foreach (var obj in objects.Where(o => o.LocationId == id))
            obj.LocationId = "";
        SaveObjects(objects);
    }
}
