namespace Bat.Core;

public interface IModifyTimeOnlyProperty : IBaseProperties
{
    TimeOnly ModifyTime { get; set; }
}