namespace Bat.Core;

public interface IModifyDateOnlyProperty : IBaseProperties
{
    DateOnly ModifyDate { get; set; }
}