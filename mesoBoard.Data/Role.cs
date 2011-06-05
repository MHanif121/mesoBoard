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
    
    public partial class Role
    {
        public Role()
        {
            this.ForumPermissions = new HashSet<ForumPermission>();
            this.InRoles = new HashSet<InRole>();
            this.UserProfiles = new HashSet<UserProfile>();
        }
    
        public int RoleID { get; set; }
        public string Name { get; set; }
        public Nullable<int> RankID { get; set; }
        public bool IsGroup { get; set; }
        public byte SpecialPermissions { get; set; }
    
        public virtual ICollection<ForumPermission> ForumPermissions { get; set; }
        public virtual ICollection<InRole> InRoles { get; set; }
        public virtual Rank Rank { get; set; }
        public virtual ICollection<UserProfile> UserProfiles { get; set; }
    }
}