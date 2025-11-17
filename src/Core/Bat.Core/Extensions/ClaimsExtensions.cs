using System.Security.Claims;

namespace Bat.Core;

public static class ClaimsExtensions
{
    extension(ClaimsPrincipal cp)
    {
        public int GetUserId_Str() => int.Parse(cp.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
        public Guid GetUserId() => Guid.Parse(cp.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
        public string GetEmail() => cp.Claims.First(x => x.Type == ClaimTypes.Email).Value;
        public string GetUsername() => cp.Claims.First(x => x.Type == ClaimTypes.Name).Value;
        public string GetFullName() => cp.Claims.First(x => x.Type == "FullName").Value;
        public string GetPicture() => cp.Claims.First(x => x.Type == "Picture").Value;
        public T GetCustomField<T>() where T : class
        {
            try
            {
                var claim = cp.Claims.FirstOrDefault(x => x.Type == "CustomField");
                if (claim == null) return null;
                return claim.Value.DeSerializeJson<T>();
            }
            catch
            {
                return null;
            }
        }
    }
}