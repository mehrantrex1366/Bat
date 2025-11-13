namespace Bat.Core;

public interface IInsertDateOnlyProperty : IBaseProperties
{
    DateOnly InsertDate { get; set; }
}