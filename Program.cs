using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Crossword
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
            builder.Services.AddSignalR();

            // Register queue and background service
            builder.Services.AddSingleton<Storage.BoardStorageService>();
            builder.Services.AddSingleton<Models.MoveQueueService>();
            builder.Services.AddHostedService<Models.MoveQueueProcessorService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }

            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".riv"] = "application/octet-stream";

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = provider
            });

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapHub<Models.GameActionsHub>("/gameActionsHub");

            app.Run();
        }
    }
}
