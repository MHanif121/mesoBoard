//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace mesoBoard.Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class Theme
    {
        public Theme()
        {
            this.UserProfiles = new HashSet<UserProfile>();
        }
    
        public int ThemeID { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool VisibleToUsers { get; set; }
        public string FolderName { get; set; }
    
        public virtual ICollection<UserProfile> UserProfiles { get; set; }
    }
}
