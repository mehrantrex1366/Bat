namespace Bat.Core;

public interface IInsertTimeOnlyProperty : IBaseProperties
{
    TimeOnly InsertTime { get; set; }
}