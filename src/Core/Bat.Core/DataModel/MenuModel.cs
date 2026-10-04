namespace Bat.Core;

public class MenuModel
{
    public int MenuId { get; set; }
    public int? ParentId { get; set; }
    public bool ShowInMenu { get; set; }
    public byte OrderPriority { get; set; }
    public string Path { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool IsDefault { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; }
    [JsonIgnore]
    public string Menus { get; set; }


    [NotMapped]
    public bool HasChild { get { return !string.IsNullOrWhiteSpace(Menus); } }

    [NotMapped]
    public bool HavePath { get { return !string.IsNullOrWhiteSpace(Path); } }

    [NotMapped]
    public List<MenuModel> ChildMenus
    {
        get
        {
            // Deserialize once per Menus value instead of on every property access
            // (it is read in loops, views and during serialization).
            var menus = Menus;
            if (_childMenus is null || !ReferenceEquals(_childMenusSource, menus))
            {
                _childMenus = (menus ?? "[]").DeSerializeJson<List<MenuModel>>();
                _childMenusSource = menus;
            }

            return _childMenus;
        }
    }

    private string _childMenusSource;
    private List<MenuModel> _childMenus;

}