namespace Bat.Test;

public interface IBehaviorDbBuilder<T> where T : BatDbContext
{
    IBehaviorDbBuilder<T> WithSaveChanges(bool isSuccess);

    T Build();
}