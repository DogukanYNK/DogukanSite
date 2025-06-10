using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DogukanSite.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IEmailSender, EmailSender>(); // Veya Scoped/Singleton
// Diğer servislerin hemen altına ekleyebilirsiniz.
builder.Services.AddScoped<ICartService, CartService>();

builder.Services.AddScoped<IOrderService, OrderService>();

// 1. Veritabanı bağlantısı
builder.Services.AddDbContext<DogukanSiteContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<DogukanSiteContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// 3. Razor Pages ve Session Servisleri
builder.Services.AddRazorPages();

builder.Services.AddDistributedMemoryCache(); // Session için dağıtılmış bellek önbelleği (eğer başka bir store kullanmıyorsanız)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // GDPR ve cookie onayı için önemli
});
builder.Services.AddHttpContextAccessor(); // Genellikle session için doğrudan gerekli değil ama başka yerlerde kullanılabilir.

var app = builder.Build();

// --- HTTP Request Pipeline Yapılandırması ---

// 5. Rol ve admin seed işlemi (Uygulama başlamadan önce bir kez çalışır)
// Bu blok app.Build() sonrasında kalabilir veya uygulamanın ilk ayağa kalkışında çalışacak şekilde farklı bir yere alınabilir.
// Önemli olan, runtime'daki middleware sıralamasını etkilememesidir.
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = new[] { "Admin", "Customer" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail = "admin@site.com"; // Bu bilgileri appsettings.json gibi bir yerden almak daha iyidir.
    var adminPassword = "Admin123!";  // Bu bilgileri appsettings.json gibi bir yerden almak daha iyidir.
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true /* Email doğrulaması gerekiyorsa */ };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        // else: admin oluşturma hatasını loglayın
    }
}

// 6. Hata Yönetimi ve Diğer Middleware'ler
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // HTTPS Zorunlu Kılma
}
else
{
    app.UseDeveloperExceptionPage(); // Geliştirme ortamında detaylı hata sayfası
    // Eğer geliştirme ortamında da özel /Error sayfanızı test etmek isterseniz:
    // app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection(); // HTTP'yi HTTPS'ye yönlendir
app.UseStaticFiles();      // Statik dosyaların (CSS, JS, resimler) sunulmasını sağlar

app.UseRouting();          // Endpoint routing'i etkinleştirir

// ÖNEMLİ: app.UseSession() burada, UseRouting'den sonra ve UseAuthentication/UseAuthorization/MapRazorPages'den önce olmalı
app.UseSession();          // Session middleware'ini etkinleştir

app.UseAuthentication();   // Kimlik doğrulama middleware'ini etkinleştir
app.UseAuthorization();    // Yetkilendirme middleware'ini etkinleştir

app.MapRazorPages();       // Razor Pages endpoint'lerini map eder

app.Run();