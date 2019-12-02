﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PostCore.Core.Activities;
using PostCore.Core.Branches;
using PostCore.Core.DbContext;
using PostCore.Core.Mail;
using PostCore.Core.Users;
using PostCore.Utils;

namespace PostCore.Core.Services.Dao
{
    public interface IMailDao
    {
        Task<IEnumerable<Post>> GetAllAsync(
            string filterId = null,
            string filterPersonFrom = null,
            string filterPersonTo = null,
            string filterAddressTo = null,
            long? filterBranchId = null,
            string filterBranchStockAddress = null,
            long? filterCarId = null,
            long? filterSourceBranchId = null,
            long? filterDestinationBranchId = null,
            PostState? filterState = null,
            string sortKey = null,
            SortOrder sortOrder = SortOrder.Ascending);
        Task CreateAsync(Post post, User user);
        Task DeliverAsync(long postId, User user);
    }

    public class MailDao : IMailDao
    {
        public static List<string> AcceptableSortKeys { get; private set; } = new List<string>
        {
            nameof(Post.Id),
            nameof(Post.PersonFrom),
            nameof(Post.PersonTo),
            nameof(Post.AddressTo),
            nameof(Post.BranchId),
            nameof(Post.BranchStockAddress),
            nameof(Post.CarId),
            nameof(Post.SourceBranchId),
            nameof(Post.DestinationBranchId),
            nameof(Post.State)
        };

        private readonly ApplicationDbContext _dbContext;
        public MailDao(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Post>> GetAllAsync(
            string filterId = null,
            string filterPersonFrom = null,
            string filterPersonTo = null,
            string filterAddressTo = null,
            long? filterBranchId = null,
            string filterBranchStockAddress = null,
            long? filterCarId = null,
            long? filterSourceBranchId = null,
            long? filterDestinationBranchId = null,
            PostState? filterState = null,
            string sortKey = null,
            SortOrder sortOrder = SortOrder.Ascending)
        {
            // 1) Check sortKey
            if (string.IsNullOrEmpty(sortKey))
            {
                sortKey = AcceptableSortKeys.First();
            }
            if (!AcceptableSortKeys.Contains(sortKey))
            {
                throw new ArgumentException("Must be one of AcceptableSortKeys", nameof(sortKey));
            }

            // 2) Filter
            var mail = _dbContext.Post
                .AsNoTracking()
                .Include(p => p.Branch)
                .Include(p => p.Car)
                .Include(p => p.SourceBranch)
                .Include(p => p.DestinationBranch)
                .AsQueryable();
            if (!string.IsNullOrEmpty(filterId))
            {
                mail = mail.Where(p => p.Id.ToString().Contains(filterId));
            }
            if (!string.IsNullOrEmpty(filterPersonFrom))
            {
                mail = mail.Where(p => p.PersonFrom.Contains(filterPersonFrom));
            }
            if (!string.IsNullOrEmpty(filterPersonTo))
            {
                mail = mail.Where(p => p.PersonTo.Contains(filterPersonTo));
            }
            if (!string.IsNullOrEmpty(filterAddressTo))
            {
                mail = mail.Where(p => p.AddressTo.Contains(filterAddressTo));
            }
            if (filterBranchId.HasValue)
            {
                mail = mail.Where(p => p.BranchId == filterBranchId.Value);
            }
            if (!string.IsNullOrEmpty(filterBranchStockAddress))
            {
                mail = mail.Where(p => p.BranchStockAddress.Contains(filterBranchStockAddress));
            }
            if (filterCarId.HasValue)
            {
                mail = mail.Where(p => p.CarId == filterCarId.Value);
            }
            if (filterSourceBranchId.HasValue)
            {
                mail = mail.Where(p => p.SourceBranchId == filterSourceBranchId.Value);
            }
            if (filterDestinationBranchId.HasValue)
            {
                mail = mail.Where(p => p.DestinationBranchId == filterDestinationBranchId.Value);
            }
            if (filterState.HasValue)
            {
                mail = mail.Where(p => p.State == filterState.Value);
            }

            // 3) Sort
            mail = mail.Order(sortKey, sortOrder);

            return await mail.ToListAsync();
        }

        public async Task CreateAsync(Post post, User user)
        {
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                post.State = PostState.Created;
                _dbContext.Post.Add(post);
                await _dbContext.SaveChangesAsync();
                _dbContext.Activity.Add(new Activity
                {
                    Type = ActivityType.PostCreated,
                    Message = $"Post {post.Id} created",
                    DateTime = DateTime.Now,
                    User = $"{user.FirstName} {user.LastName}",
                    PostId = post.Id,
                    BranchId = post.SourceBranch.Id
                });
                await _dbContext.SaveChangesAsync();

                try
                {
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
        }

        public async Task DeliverAsync(long postId, User user)
        {
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                var post = await _dbContext.Post.Where(p => p.Id == postId).FirstOrDefaultAsync();
                if (post == null)
                {
                    throw new ArgumentException("Post with such id not found", nameof(postId));
                }

                post.BranchId = null;
                post.BranchStockAddress = null;
                post.CarId = null;
                post.State = PostState.Delivered;
                _dbContext.Activity.Add(new Activity
                {
                    Type = ActivityType.PostDelivered,
                    Message = $"Post {post.Id} delivered",
                    DateTime = DateTime.Now,
                    User = $"{user.FirstName} {user.LastName}",
                    PostId = post.Id,
                    BranchId = post.DestinationBranchId
                });
                await _dbContext.SaveChangesAsync();

                try
                {
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
        }
    }
}