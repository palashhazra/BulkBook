﻿using BulkBook.DataAccessLayer.Infrastructure.IRepository;
using BulkBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkBook.DataAccessLayer.Infrastructure.Repository
{
	public class ApplicationRepository : Repository<ApplicationUser>, IApplicationUser
	{
		private ApplicationDbContext _context;

		public ApplicationRepository(ApplicationDbContext context) : base(context)
		{
			_context = context;
		}		
	}
}
