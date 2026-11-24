namespace Tracker.Session;

// The release protocol needs a counter that only ever moves forward and never
// repeats a value, even across restarts. Reserving means: hand out the current
// value and persist the next one before returning.
public interface ICounterStore
{
    long Reserve();
}
