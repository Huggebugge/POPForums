using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using PopForums.Mvc.Areas.Forums.Services;
using System;
using System.IO;

namespace PopForums.Mvc.Areas.Forums.Controllers
{
	[Area("Forums")]
	public class UploadController : Controller
	{
		public UploadController(IWebHostEnvironment hostingEnvironment, IUserRetrievalShim userRetrievalShim)
		{
			_hostingEnvironment = hostingEnvironment;
			_userRetrievalShim = userRetrievalShim;
		}
		private readonly IWebHostEnvironment _hostingEnvironment;
		private readonly IUserRetrievalShim _userRetrievalShim;
		public static string Name = "Upload";
		public JsonResult UploadFile()
		{
			if (!_userRetrievalShim.GetUser().IsApproved)
			{
				return null;
			}
			string fileName = "";
			string folderName = "uploads/";
			Microsoft.AspNetCore.Http.IFormFile file;
			try
			{
				file = Request.Form.Files[0];
				string webRootPath = _hostingEnvironment.WebRootPath;
				string newPath = Path.Combine(webRootPath, folderName);
				if (!Directory.Exists(newPath))
				{
					Directory.CreateDirectory(newPath);
				}
				if (file.Length > 0)
				{
					fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid()}.{Path.GetExtension(file.FileName)}";
					string fullPath = Path.Combine(newPath, fileName);
					using (var stream = new FileStream(fullPath, FileMode.Create))
					{
						file.CopyTo(stream);
					}
				}
				return Json(new { location = $"{fileName}" });
			}
			catch (System.Exception ex)
			{
				return Json("Upload Failed: " + ex.Message);
			}
		}
	}
}
