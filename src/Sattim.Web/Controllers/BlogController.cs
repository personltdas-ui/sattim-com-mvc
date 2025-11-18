using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Blog;
using Sattim.Web.ViewModels.Blog;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("Blog")]
public class BlogController : BaseController
{
    private readonly IBlogService _blogService;
    private readonly UserManager<ApplicationUser> _userManager;

    private const int PageSize = 10;

    public BlogController(
        IBlogService blogService,
        UserManager<ApplicationUser> userManager)
    {
        _blogService = blogService;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(int page = 1, [FromQuery] string? tag = null)
    {
        var (posts, totalPages) = await _blogService.GetPublishedPostsAsync(page, PageSize, tag);

        var tagCloudData = await _blogService.GetTagCloudAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.CurrentTag = tag;

        ViewBag.TagCloudData = tagCloudData;

        return View(posts);
    }

    [HttpGet("Post/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return BadRequest();
        }

        try
        {
            var post = await _blogService.GetPostBySlugAsync(slug);

            var tagCloudData = await _blogService.GetTagCloudAsync();

            var commentFormModel = new PostCommentViewModel
            {
                BlogPostId = post.Id
            };
            ViewBag.CommentForm = commentFormModel;

            ViewBag.TagCloudData = tagCloudData;

            return View(post);
        }
        catch (KeyNotFoundException)
        {
            TempData["ErrorMessage"] = "Aradığınız yazı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("PostComment/{slug}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostComment(string slug, [FromForm] PostCommentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Yorumunuz 10 karakterden uzun olmalıdır.";
            return RedirectToAction(nameof(Details), new { slug = slug });
        }

        var userId = _userManager.GetUserId(User);
        var (success, returnedSlug, errorMessage) = await _blogService.PostCommentAsync(model, userId);

        if (success)
        {
            TempData["SuccessMessage"] = "Yorumunuz başarıyla gönderildi ve onaya alındı.";
            return RedirectToAction(nameof(Details), new { slug = returnedSlug });
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(Details), new { slug = slug });
        }
    }

    [HttpGet("TagCloud")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTagCloudPartial()
    {
        var tags = await _blogService.GetTagCloudAsync();
        return PartialView("_TagCloudPartial", tags);
    }
}