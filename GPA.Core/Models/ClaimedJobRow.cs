using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Core.Models
{
    [Keyless]
    public class ClaimedJobRow
    {
        public Guid Id { get; set; }
    }
}
