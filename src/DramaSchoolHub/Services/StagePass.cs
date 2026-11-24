namespace DramaSchoolHub.Services;

// Used by the Lifetimes page. Each pass gets a random code when it's created,
// so two passes with the same code are the same object.
public abstract class StagePass
{
    public string Code { get; } = Guid.NewGuid().ToString()[..6];
}

// One subclass per lifetime: the container needs a separate type for each registration.
public class TransientPass : StagePass { }
public class ScopedPass : StagePass { }
public class SingletonPass : StagePass { }
