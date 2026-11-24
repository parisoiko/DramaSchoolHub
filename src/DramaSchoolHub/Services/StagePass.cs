namespace DramaSchoolHub.Services;

// abstract: nobody creates a plain StagePass, only the three kinds below.
public abstract class StagePass
{
    // A random code, fixed when the object is created. [..6] keeps the first 6 characters.
    public string Code { get; } = Guid.NewGuid().ToString()[..6];
}

public class TransientPass : StagePass { }
public class ScopedPass : StagePass { }
public class SingletonPass : StagePass { }