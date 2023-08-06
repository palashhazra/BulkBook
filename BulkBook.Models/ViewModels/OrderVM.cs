using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkBook.Models.ViewModels
{
    public class OrderVM
    {
        public OrderHeader OrderHeader { get; set; }   //to show basic info related to order
        public IEnumerable<OrderDetail> OrderDetail { get; set; } //a user can hold multiple orders
    }
}
