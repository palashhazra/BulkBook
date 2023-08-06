using BulkBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkBook.DataAccessLayer.Infrastructure.IRepository
{
	public interface IOrderHeaderRepository : IRepository<OrderHeader>
	{		
		void Update(OrderHeader orderHeader);
		void UpdateStatus(int Id, string orderStatus, string? paymentStatus=null);
		void PaymentStatus(int Id, string SessionId, string PaymentIntentId);
	}
}
