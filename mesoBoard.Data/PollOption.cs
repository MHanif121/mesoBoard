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
    
    public partial class PollOption
    {
        public PollOption()
        {
            this.PollVotes = new HashSet<PollVote>();
        }
    
        public int PollOptionID { get; set; }
        public string Text { get; set; }
        public int PollID { get; set; }
    
        public virtual Poll Poll { get; set; }
        public virtual ICollection<PollVote> PollVotes { get; set; }
    }
}
