using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Xml.Linq;
using mesoBoard.Common;
using mesoBoard.Data;
using mesoBoard.Framework;
using mesoBoard.Framework.Core;
using mesoBoard.Framework.Models;
using mesoBoard.Services;

namespace mesoBoard.Web.Controllers
{
    public class BoardController : BaseController
    {
        private ForumServices _forumServices;
        private SearchServices _searchServices;
        private ThreadServices _threadServices;
        private PostServices _postServices;
        private PollServices _pollServices;
        private IRepository<OnlineUser> _onlineUserRepository;
        private IRepository<OnlineGuest> _onlineGuestRepository;
        private IRepository<User> _userRepository;
        private UserServices _userServices;
        private RoleServices _roleServices;
        private MessageServices _messageServices;
        private PermissionServices _permissionServices;
        private FileServices _fileServices;
        private User _currentUser;
        private Theme _currentTheme;

        public BoardController(
            ForumServices forumServices,
            SearchServices searchServices,
            ThreadServices threadServices,
            PostServices postServices,
            PollServices pollServices,
            GlobalServices globalServices,
            IRepository<OnlineUser> onlineUserRepository,
            IRepository<OnlineGuest> onlineGuestRepository,
            IRepository<User> userRepository,
            UserServices usersServices,
            RoleServices roleServices,
            MessageServices messageServices,
            PermissionServices permissionServices,
            FileServices fileServices,
            User currentUser,
            Theme currentTheme)
        {
            _forumServices = forumServices;
            _searchServices = searchServices;
            _threadServices = threadServices;
            _postServices = postServices;
            _pollServices = pollServices;
            _onlineUserRepository = onlineUserRepository;
            _onlineGuestRepository = onlineGuestRepository;
            _userRepository = userRepository;
            _userServices = usersServices;
            _roleServices = roleServices;
            _messageServices = messageServices;
            _permissionServices = permissionServices;
            _fileServices = fileServices;
            _currentUser = currentUser;
            _currentTheme = currentTheme;
            SetTopBreadCrumb("Board");
        }

        [DefaultAction]
        public ActionResult Index()
        {
            SetBreadCrumb("Board Index");

            IEnumerable<Forum> forums = _forumServices.GetViewableForums(_currentUser.UserID);

            IEnumerable<ForumRow> forumRows = forums.Select(x => new ForumRow
            {
                Forum = x,
                HasNewPost = _forumServices.HasNewPost(x.ForumID, _currentUser.UserID),
                LastPost = _forumServices.GetLastPost(x.ForumID)
            });

            IEnumerable<Category> categories = forums.Select(x => x.Category).Distinct().OrderBy(item => item.Order);

            var model = new BoardIndexViewModel()
            {
                Categories = categories,
                Forums = forumRows
            };

            return View(model);
        }

        public ActionResult Confirm(string yesRedirect, string noRedirect)
        {
            SetCrumb("Confirm Operation");
            ViewData["YesUrl"] = yesRedirect;
            ViewData["NoUrl"] = noRedirect;
            return View();
        }

        public ActionResult Search(string Keywords, int Page = 1, int PageSize = 10)
        {
            SetBreadCrumb("Search");

            if (string.IsNullOrEmpty(Keywords))
            {
                if (Request.HttpMethod.Equals("Post", StringComparison.InvariantCultureIgnoreCase))
                    SetNotice("Enter a keyword to search");
                return View();
            }

            IEnumerable<Post> results = _searchServices.SearchPosts(Keywords);
            List<Post> result = results.ToList();
            int count = result.Count;
            result = result.TakePage(Page, PageSize).ToList();

            Pagination pagination = new Pagination(Page, count, PageSize, "Search", "Board", new { Keywords = Keywords });

            ViewData["Pagination"] = pagination;
            return View(result);
        }

        public ActionResult ViewForum(int ForumID, int Page = 1, bool LastPost = false)
        {
            Forum forum = _forumServices.GetForum(ForumID);
            UserPermissions userPermissions = _permissionServices.GetUserPermissions(forum.ForumID, _currentUser.UserID);

            if (!userPermissions.Visible)
            {
                SetNotice("You can't view this forum");

                return RedirectToAction("Index", "Board");
            }

            int threadCount = forum.Threads.Count;
            int pageSize = SiteConfig.ThreadsPerPage.ToInt();
            int pageCount = (int)Math.Ceiling(((decimal)threadCount / pageSize));
            int currentPage = LastPost ? pageCount : Page;

            SetBreadCrumb(forum.Name);

            Pagination forumPagination = new Pagination(currentPage, threadCount, pageSize, "ViewForum", "Board", new { ForumID = ForumID });

            IEnumerable<Thread> forumThreads = _threadServices.GetPagedThreads(ForumID, currentPage, pageSize).Where(y => y.Type != 4);

            IEnumerable<ThreadRow> threadRows = forumThreads.ToList().Select((thread, index) => new ThreadRow
            {
                IsOdd = index % 2 != 0,
                Thread = thread,
                TotalPosts = thread.Posts.Count,
                FirstPost = thread.FirstPost,
                LastPost = thread.Posts.OrderByDescending(post => post.Date).FirstOrDefault(),
                CurrentUser = _currentUser,
                HasNewPost = Request.IsAuthenticated ? _threadServices.HasNewPost(thread.ThreadID, _currentUser.UserID) : false,
                IsSubscribed = Request.IsAuthenticated ? _threadServices.IsSubscribed(thread.ThreadID, _currentUser.UserID) : false,
                HasAttachment = _threadServices.HasAttachment(thread.ThreadID)
            });

            IEnumerable<Thread> globalAnnouncements = _threadServices.GetGlobalAnnouncements();

            IEnumerable<ThreadRow> gaThreads = globalAnnouncements.Select((thread, index) => new ThreadRow
            {
                IsOdd = index % 2 != 0,
                Thread = thread,
                TotalPosts = thread.Posts.Count,
                FirstPost = thread.FirstPost,
                LastPost = thread.Posts.OrderByDescending(post => post.Date).FirstOrDefault(),
                CurrentUser = _currentUser,
                HasNewPost = Request.IsAuthenticated ? _threadServices.HasNewPost(thread.ThreadID, _currentUser.UserID) : false,
                IsSubscribed = Request.IsAuthenticated ? _threadServices.IsSubscribed(thread.ThreadID, _currentUser.UserID) : false
            });

            ViewForumViewModel viewForum = new ViewForumViewModel()
            {
                Forum = forum,
                ThreadRows = threadRows.ToList(),
                GlobalAnnouncements = gaThreads.ToList(),
                Pagination = forumPagination,
                UserPermissions = userPermissions
            };

            return View(viewForum);
        }

        public ActionResult ViewThread(int ThreadID, int? PostID, bool LastPost = false, int Page = 1)
        {
            Thread thread = _threadServices.GetThread(ThreadID);

            if (!_permissionServices.CanView(thread.ForumID, _currentUser.UserID) && thread.Type != 4)
            {
                SetNotice("You can't view this forum");
                return RedirectToAction("Index");
            }

            if (User.Identity.IsAuthenticated)
                _threadServices.ThreadViewed(ThreadID, _currentUser.UserID);

            int PostCount = thread.Posts.Count;
            int PageSize = int.Parse(SiteConfig.PostsPerPage.Value);
            int PageCount = (int)Math.Ceiling(((decimal)PostCount / PageSize));
            int CurrentPage = LastPost ? PageCount : Page;

            if (PostID != null)
            {
                Post post = _postServices.GetPost(PostID.Value);
                IEnumerable<Post> AllPosts = _threadServices.GetPosts(ThreadID);
                int Position = AllPosts.ToList().IndexOf(post) + 1;
                CurrentPage = (int)Math.Ceiling(((decimal)Position / PageSize));
            }

            SetBreadCrumb(thread.Title);

            Pagination Pagination = new Pagination(CurrentPage, PostCount, PageSize, "ViewThread", "Board", new { ThreadID = ThreadID });

            int lastPostId = thread.Posts.OrderByDescending(item => item.Date).FirstOrDefault().PostID;

            IEnumerable<Post> PagedPosts = _postServices.GetPagedPosts(ThreadID, CurrentPage, PageSize);

            bool hasPermissions = _roleServices.UserHasSpecialPermissions(_currentUser.UserID, SpecialPermissionValue.Administrator, SpecialPermissionValue.Moderator);
            bool canReply = _permissionServices.CanReply(thread.ForumID, _currentUser.UserID);

            IEnumerable<PostRow> postRows = PagedPosts.Select((x, i) => new PostRow
            {
                Post = x,
                CurrentUser = _currentUser,
                Thread = thread,
                CanEdit = hasPermissions ? true : (x.UserID == _currentUser.UserID ? _postServices.CanEditPost(x.PostID, _currentUser.UserID) : false),
                CanDelete = hasPermissions ? true : (x.UserID == _currentUser.UserID ? _postServices.CanDeletePost(x.PostID, _currentUser.UserID) : false),
                CanPost = canReply,
                IsLastPost = x.PostID == lastPostId,
                IsOdd = i % 2 != 0,
                CurrentTheme = _currentTheme,
                IsAuthenticated = User.Identity.IsAuthenticated
            });

            var model = new ViewThreadViewModel
            {
                Thread = thread,
                Posts = postRows,
                Pagination = Pagination,
                CanCastVote = _permissionServices.CanCastVote(thread.ForumID, _currentUser.UserID),
                HasVoted = _pollServices.HasVoted(thread.ThreadID, _currentUser.UserID),
                IsSubscribed = _threadServices.IsSubscribed(thread.ThreadID, _currentUser.UserID),
                ThreadActions = new ThreadActions
                {
                    CanLock = _permissionServices.CanLock(thread.ThreadID, _currentUser.UserID),
                    CurrentUser = _currentUser,
                    Thread = thread
                },
                CurrentUser = _currentUser,
            };

            if (thread.HasPoll)
            {
                var threadPoll = new ThreadPoll()
                {
                    CanCastVote = model.CanCastVote && !model.HasVoted && !thread.IsLocked,
                    CurrentUser = _currentUser,
                    Poll = thread.Poll
                };

                model.ThreadPoll = threadPoll;
            }

            return View(model);
        }

        [Authorize]
        public ActionResult ToggleThreadSubscription(int threadID)
        {
            Thread thread = _threadServices.GetThread(threadID);

            if (!_permissionServices.CanView(thread.ForumID, _currentUser.UserID) && thread.Type != 4)
            {
                return RedirectToAction("Index", "Board");
            }

            if (_threadServices.ToggleThreadSubscription(threadID, _currentUser.UserID))
                SetSuccess("You have subscribed to thread");
            else
                SetSuccess("You have unsubscribed to thread");

            return RedirectToAction("ViewThread", new { ThreadID = threadID });
        }

        [PermissionAuthorize(SpecialPermissionValue.Administrator, SpecialPermissionValue.Moderator)]
        public ActionResult ToggleThreadLock(int threadID)
        {
            if (_threadServices.ToggleThreadLock(threadID))
                SetSuccess("Thread locked");
            else
                SetSuccess("Thread unlocked");

            return RedirectToAction("ViewThread", new { ThreadID = threadID });
        }

        public ActionResult BoardStats()
        {
            var onlineUsers = _onlineUserRepository.Get().Select(item =>
                new OnlineUserDetails()
                {
                    DefaultRole = item.User.UserProfile.DefaultRole.HasValue ? item.User.UserProfile.Role : null,
                    OnlineUser = item
                }).ToList();

            IEnumerable<OnlineGuest> onlineGuests = _onlineGuestRepository.Get().ToList();

            var newestUser = _userRepository.Get().OrderByDescending(item => item.RegisterDate).First();
            var birthdays = _userServices.GetBirthdays(DateTime.UtcNow);

            var model = new BoardStatsViewModel()
            {
                NewestUser = newestUser,
                OnlineGuests = onlineGuests,
                OnlineUsers = onlineUsers,
                TotalPosts = _postServices.TotalPosts(),
                TotalRegisteredUsers = _userRepository.Get().Count(),
                TotalThreads = _threadServices.TotalThreads(),
                BirthdayUsers = birthdays
            };

            return View(model);
        }

        public ActionResult DownloadAttachment(int AttachmentID)
        {
            Attachment attachment = _fileServices.GetAttachment(AttachmentID);
            string path = Server.MapPath(Path.Combine(DirectoryPaths.Attachments, attachment.SavedName));
            if (System.IO.File.Exists(path))
            {
                if (!_permissionServices.CanDownloadAttachment(attachment.Post.Thread.ForumID, _currentUser.UserID))
                {
                    SetNotice("You don't have permission to download this file");
                    return RedirectToAction("ViewThread", "Board", new { ThreadID = attachment.Post.ThreadID });
                }
                _fileServices.LogAttachmentDownload(AttachmentID);
                return File(path, attachment.Type, attachment.DownloadName);
            }
            else
            {
                SetNotice("File not found");
                return RedirectToAction("ViewThread", "Board", new { ThreadID = attachment.Post.ThreadID });
            }
        }

        [AllowOffline]
        public ActionResult Offline()
        {
            return View();
        }

        [ChildActionOnly]
        [AllowOffline]
        public ActionResult Header()
        {
            HeaderViewModel model = new HeaderViewModel()
            {
                CurrentUser = _currentUser,
                NewMessagesCount = Request.IsAuthenticated ? _messageServices.GetUnreadMessages(_currentUser.UserID).Count() : 0,
                IsAdministrator = Request.IsAuthenticated ? _roleServices.UserHasSpecialPermissions(_currentUser.UserID, SpecialPermissionValue.Administrator) : false
            };

            return View("_Header", model);
        }
    }
}