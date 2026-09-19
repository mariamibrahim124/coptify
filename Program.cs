using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;
using Coptify.Web.Data;
using Coptify.Web.Models;
using Coptify.Web.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Use PostgreSQL in deployed environments and the checked-in SQLite database locally.
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseSqlite("Data Source=coptify.db"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseNpgsql(connectionString));
}

builder.Services.AddControllers();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "coptify_admin";
        o.LoginPath = "/admin.html";
        o.AccessDeniedPath = "/admin.html";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Create PostgreSQL tables if they don't exist
    db.Database.EnsureCreated();

    EnsureAdminTable(db);
    Seed(db);
    SeedAutoContent(db, app.Environment.WebRootPath);
    NormalizeContactEmails(db);
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/admin", () => Results.Redirect("/admin.html"));

app.MapFallbackToFile("index.html");

app.Run();


// ============================================================
// ADMIN
// ============================================================

static void EnsureAdminTable(AppDbContext db)
{
    // PostgreSQL tables are created by EF Core EnsureCreated().
    // This method only makes sure the default admin user exists.

    if (!db.AdminUsers.Any())
    {
        var username = Environment.GetEnvironmentVariable("COPTIFY_ADMIN_USERNAME")
            ?? "admin";

        var password = Environment.GetEnvironmentVariable("COPTIFY_ADMIN_PASSWORD")
            ?? "ChangeMe123!";

        var p = PasswordService.Create(password);

        db.AdminUsers.Add(new AdminUser
        {
            Username = username,
            PasswordSalt = p.Salt,
            PasswordHash = p.Hash,
            UpdatedAtUtc = DateTime.UtcNow
        });

        db.SaveChanges();
    }
}


// ============================================================
// AUTO CONTENT
// ============================================================

static void SeedAutoContent(AppDbContext db, string webRootPath)
{
    var path = Path.Combine(webRootPath, "index.html");

    if (!File.Exists(path))
        return;

    var html = File.ReadAllText(path);

    var rx = new Regex(
        """<(?<tag>[a-zA-Z0-9]+)(?<attrs>[^>]*data-cms-key="(?<key>[^"]+)"[^>]*)>(?<value>[^<]*)</\k<tag>>""",
        RegexOptions.Singleline
    );

    var existing = db.SiteContents
        .Select(x => x.Key)
        .ToHashSet();

    foreach (Match m in rx.Matches(html))
    {
        var key = m.Groups["key"].Value;

        if (!key.StartsWith("auto.text.") || existing.Contains(key))
            continue;

        var value = WebUtility.HtmlDecode(
            m.Groups["value"].Value
        ).Trim();

        if (string.IsNullOrWhiteSpace(value))
            continue;

        db.SiteContents.Add(new SiteContent
        {
            Key = key,
            Label = "واجهة — " + value,
            Section = "واجهة الموقع",
            Value = value
        });

        existing.Add(key);
    }

    db.SaveChanges();
}

static void NormalizeContactEmails(AppDbContext db)
{
    var changed = false;

    foreach (var item in db.SiteContents.Where(x =>
        x.Value.Contains("hello@coptify.com") || x.Label.Contains("hello@coptify.com")))
    {
        item.Value = item.Value.Replace("hello@coptify.com", "coptify.page@gmail.com");
        item.Label = item.Label.Replace("hello@coptify.com", "coptify.page@gmail.com");
        changed = true;
    }

    if (changed)
        db.SaveChanges();
}


// ============================================================
// SEED
// ============================================================

static void Seed(AppDbContext db)
{
    if (!db.Videos.Any())
    {
        db.Videos.AddRange(
            new Video
            {
                Title = "فيديو تجريبي 1",
                Description = "عدّلي هذا الفيديو من لوحة الإدارة.",
                Category = "فيديوهات",
                EpisodeNumber = "1",
                VideoUrl = "https://www.facebook.com/",
                Published = true,
                SortOrder = 1
            },

            new Video
            {
                Title = "فيديو تجريبي 2",
                Description = "عدّلي هذا الفيديو من لوحة الإدارة.",
                Category = "فيديوهات",
                EpisodeNumber = "2",
                VideoUrl = "https://www.facebook.com/",
                Published = true,
                SortOrder = 2
            }
        );
    }

    if (!db.SiteContents.Any())
    {
        db.SiteContents.AddRange(new[]
        {
            new SiteContent
            {
                Key = "home.title",
                Label = "العنوان الرئيسي",
                Section = "الرئيسية",
                Value = "كوبتيفاي"
            },

            new SiteContent
            {
                Key = "home.description",
                Label = "وصف الصفحة الرئيسية",
                Section = "الرئيسية",
                Value = "منصة التاريخ والتراث والهوية القبطية الأرثوذكسية"
            },

            new SiteContent
            {
                Key = "episodes.title",
                Label = "عنوان الحلقات",
                Section = "الحلقات",
                Value = "الحلقات"
            },

            new SiteContent
            {
                Key = "timeline.title",
                Label = "عنوان الخط الزمني",
                Section = "الخط الزمني",
                Value = "الخط الزمني"
            },

            new SiteContent
            {
                Key = "fathers.title",
                Label = "عنوان آباء الكنيسة",
                Section = "آباء الكنيسة",
                Value = "آباء الكنيسة"
            },

            new SiteContent
            {
                Key = "glossary.title",
                Label = "عنوان المعاجم",
                Section = "المعاجم",
                Value = "المعاجم"
            },

            new SiteContent
            {
                Key = "articles.title",
                Label = "عنوان المقالات",
                Section = "المقالات",
                Value = "المقالات"
            },

            new SiteContent
            {
                Key = "about.title",
                Label = "عنوان عن كوبتيفاي",
                Section = "عن كوبتيفاي",
                Value = "عن كوبتيفاي"
            },

            new SiteContent
            {
                Key = "faq.title",
                Label = "عنوان الأسئلة الشائعة",
                Section = "الأسئلة الشائعة",
                Value = "الأسئلة الشائعة"
            },

            new SiteContent
            {
                Key = "newsletter.title",
                Label = "عنوان النشرة البريدية",
                Section = "النشرة البريدية",
                Value = "اشترك في النشرة البريدية"
            },

            new SiteContent
            {
                Key = "page.text.1",
                Label = "نص الصفحة 1",
                Section = "home",
                Value = "ذاكرةُ الكنيسة، من القرن الأول إلى اليوم"
            },

            new SiteContent
            {
                Key = "page.text.2",
                Label = "نص الصفحة 2",
                Section = "home",
                Value = "كوبتيفاي منصة عربية توثيقية تروي تاريخ الكنيسة القبطية الأرثوذكسية وتراثها وهُويتها؛ عبر حلقات أسبوعية، وخطٍّ زمني تفاعلي، ودليلٍ لآباء الكنيسة، ومعجمٍ للمصطلحات الطقسية، ومقالاتٍ معمّقة للأجيال القادمة."
            },

            new SiteContent
            {
                Key = "page.text.3",
                Label = "نص الصفحة 3",
                Section = "home",
                Value = "القديس أثناسيوس الرسولي: خمسُ منافٍ في سبيل كلمة واحدة"
            },

            new SiteContent
            {
                Key = "page.text.4",
                Label = "نص الصفحة 4",
                Section = "home",
                Value = "من مجمع نيقية إلى صحاري مصر، كيف دافع شابٌّ في الثلاثين عن «الإيمان المستقيم» أمام عالمٍ كامل؟ رحلة توثيقية في سيرة «أثناسيوس ضد العالم»."
            },

            new SiteContent
            {
                Key = "page.text.5",
                Label = "نص الصفحة 5",
                Section = "episodes",
                Value = "الحلقات المنشورة تُدار بالكامل من لوحة الإدارة."
            },

            new SiteContent
            {
                Key = "page.text.6",
                Label = "نص الصفحة 6",
                Section = "timeline",
                Value = "عشرون قرنًا في خطٍّ واحد"
            },

            new SiteContent
            {
                Key = "page.text.7",
                Label = "نص الصفحة 7",
                Section = "timeline",
                Value = "اضغط على أي محطة لتقرأ تفاصيلها، ثم انتقل إلى الحلقة المرتبطة بها. من كاروزية مارمرقس إلى اليوم."
            },

            new SiteContent
            {
                Key = "page.text.8",
                Label = "نص الصفحة 8",
                Section = "timeline",
                Value = "وصول مارمرقس إلى الإسكندرية"
            },

            new SiteContent
            {
                Key = "page.text.9",
                Label = "نص الصفحة 9",
                Section = "timeline",
                Value = "بحسب التقليد الكنسي، وصل مارمرقس الرسول إلى الإسكندرية نحو عام 42م، فكان أول من بشّر بالمسيحية في مصر وأسّس الكرسي الإسكندري، الذي صار أحد أقدم الكراسي الرسولية في العالم."
            },

            new SiteContent
            {
                Key = "page.text.10",
                Label = "نص الصفحة 10",
                Section = "timeline",
                Value = "مدرسة الإسكندرية اللاهوتية"
            },

            new SiteContent
            {
                Key = "page.text.11",
                Label = "نص الصفحة 11",
                Section = "timeline",
                Value = "نشأت مدرسة الإسكندرية كأول مؤسسة تعليمية لاهوتية منهجية في التاريخ المسيحي. تخرّج منها بانتينوس وكليمندس وأوريجانوس، وصارت جسرًا بين الفلسفة اليونانية والفكر المسيحي."
            },

            new SiteContent
            {
                Key = "page.text.12",
                Label = "نص الصفحة 12",
                Section = "timeline",
                Value = "عصر الشهداء الكبير"
            },

            new SiteContent
            {
                Key = "page.text.13",
                Label = "نص الصفحة 13",
                Section = "timeline",
                Value = "خلال الاضطهاد الدقلدياني، قدّمت الكنيسة القبطية عددًا هائلًا من الشهداء، لدرجة أن التقويم القبطي يعرف بـ«تقويم الشهداء» ويبدأ من عام 284م — سنة اعتلاء دقلديانوس العرش."
            },

            new SiteContent
            {
                Key = "page.text.14",
                Label = "نص الصفحة 14",
                Section = "timeline",
                Value = "مجمع نيقية وأثناسيوس الرسولي"
            },

            new SiteContent
            {
                Key = "page.text.15",
                Label = "نص الصفحة 15",
                Section = "timeline",
                Value = "انعقد أول مجمع مسكوني في نيقية لمواجهة الآريوسية، وحضره 318 أسقفًا من بينهم القديس أثناسيوس الشماس. صيغ فيه قانون الإيمان الذي ما زالت الكنيسة تردده حتى اليوم."
            },

            new SiteContent
            {
                Key = "page.text.16",
                Label = "نص الصفحة 16",
                Section = "timeline",
                Value = "مجمع خلقيدونية"
            },

            new SiteContent
            {
                Key = "page.text.17",
                Label = "نص الصفحة 17",
                Section = "timeline",
                Value = "انعقد المجمع الرابع في خلقيدونية، ورفضته الكنيسة القبطية مع عدد من الكنائس الشرقية، وتمسّكت بصيغة القديس كيرلس الكبير في «طبيعة واحدة متجسدة لله الكلمة». ومن هنا بدأ مسارها المستقل ككنيسة أرثوذكسية شرقية."
            },

            new SiteContent
            {
                Key = "page.text.18",
                Label = "نص الصفحة 18",
                Section = "timeline",
                Value = "الفتح العربي لمصر"
            },

            new SiteContent
            {
                Key = "page.text.19",
                Label = "نص الصفحة 19",
                Section = "timeline",
                Value = "دخلت الجيوش العربية مصر، وبدأ عصرٌ جديد تحوّلت فيه اللغة والثقافة تدريجيًا من القبطية إلى العربية. ومع ذلك بقيت القبطية لغة الليتورجيا، وانتقلت الكتابة اللاهوتية إلى العربية."
            },

            new SiteContent
            {
                Key = "page.text.20",
                Label = "نص الصفحة 20",
                Section = "timeline",
                Value = "انتقال الكرسي البطريركي إلى القاهرة"
            },

            new SiteContent
            {
                Key = "page.text.21",
                Label = "نص الصفحة 21",
                Section = "timeline",
                Value = "مع تأسيس القاهرة، انتقل مقر البطريرك القبطي من الإسكندرية إلى القاهرة، حيث بقي حتى اليوم، مع احتفاظ البطريرك بلقب «بابا الإسكندرية». وشهدت هذه الفترة ازدهارًا في الأدب القبطي العربي."
            },

            new SiteContent
            {
                Key = "page.text.22",
                Label = "نص الصفحة 22",
                Section = "timeline",
                Value = "الكلية الإكليريكية والنهضة الحديثة"
            },

            new SiteContent
            {
                Key = "page.text.23",
                Label = "نص الصفحة 23",
                Section = "timeline",
                Value = "تأسست الكلية الإكليريكية بالقاهرة لتعليم اللاهوت واللغات القبطية، فكانت نقطة انطلاق نهضة تعليمية وثقافية شاملة، صاحبتها حركة مدارس الأحد وجمعيات الكنيسة."
            },

            new SiteContent
            {
                Key = "page.text.24",
                Label = "نص الصفحة 24",
                Section = "timeline",
                Value = "عصر البابا كيرلس السادس"
            },

            new SiteContent
            {
                Key = "page.text.25",
                Label = "نص الصفحة 25",
                Section = "timeline",
                Value = "بعد سنوات من الرهبنة في طاحونة المقطم، صار الراهب مينا البابا كيرلس السادس (1959–1971)، فشهدت الكنيسة نهضة رهبانية وخدمية واسعة، وامتدّت الكنيسة القبطية إلى أفريقيا وأوروبا والأمريكتين."
            },

            new SiteContent
            {
                Key = "page.text.26",
                Label = "نص الصفحة 26",
                Section = "timeline",
                Value = "الأقباط في القرن الحادي والعشرين"
            },

            new SiteContent
            {
                Key = "page.text.27",
                Label = "نص الصفحة 27",
                Section = "timeline",
                Value = "يعيش الأقباط اليوم داخل مصر ومهاجرين في أنحاء العالم، ويواجهون سؤال الهوية من زاوية جديدة: كيف نحفظ اللغة واللحن والطقس في عالم رقمي سريع؟ ومن هنا جاءت فكرة كوبتيفاي."
            },

            new SiteContent
            {
                Key = "page.text.28",
                Label = "نص الصفحة 28",
                Section = "fathers",
                Value = "رجالٌ صاروا حجرًا في الأساس"
            },

            new SiteContent
            {
                Key = "page.text.29",
                Label = "نص الصفحة 29",
                Section = "fathers",
                Value = "بطاقات تعريفية لأبرز آباء الكنيسة القبطية ومعلميها؛ سيرة موجزة، وأقوال باقية، والحلقة المرتبطة بكلٍّ منهم."
            },

            new SiteContent
            {
                Key = "page.text.30",
                Label = "نص الصفحة 30",
                Section = "fathers",
                Value = "مارمرقس الرسول"
            },

            new SiteContent
            {
                Key = "page.text.31",
                Label = "نص الصفحة 31",
                Section = "fathers",
                Value = "واحد من السبعين رسولًا، كاتب الإنجيل الثاني. بشّر في الإسكندرية وأسّس كنيسة مصر، ثم استُشهد فيها"
            },

            new SiteContent
            {
                Key = "page.text.32",
                Label = "نص الصفحة 32",
                Section = "fathers",
                Value = "وُلد يوحنا الملقّب مرقس في قبرص أو في أورشليم، ورافق بولس وبرنابا في رحلاتهما التبشيرية، ثم صار تلميذًا وكاتبًا لبطرس الرسول الذي دعاه «ابني مرقس»."
            },

            new SiteContent
            {
                Key = "page.text.33",
                Label = "نص الصفحة 33",
                Section = "fathers",
                Value = "كتب إنجيله في روما، ثم توجّه إلى مصر نحو عام 42م، فدخل الإسكندرية من جهة الميناء، وبدأ يبشّر فيها. تشير المصادر الكنسية إلى أنه رسم أنيانوس أسقفًا للإسكندرية، ثم عاد إلى الخمس مدن الغربية، ومنها إلى الإسكندرية مرة أخرى حيث استُشهد."
            },

            new SiteContent
            {
                Key = "page.text.34",
                Label = "نص الصفحة 34",
                Section = "fathers",
                Value = "محطات في سيرته"
            },

            new SiteContent
            {
                Key = "page.text.35",
                Label = "نص الصفحة 35",
                Section = "fathers",
                Value = "القديس أنطونيوس الكبير"
            },

            new SiteContent
            {
                Key = "page.text.36",
                Label = "نص الصفحة 36",
                Section = "fathers",
                Value = "مؤسس الرهبنة المسيحية. ترك الميراث وذهب إلى الصحراء الشرقية، فصار قدوةً للرهبان في العالم كله"
            },

            new SiteContent
            {
                Key = "page.text.37",
                Label = "نص الصفحة 37",
                Section = "fathers",
                Value = "وُلد في قرية قِمن العروس ببني سويف نحو عام 251م من أسرة مسيحية ميسورة. بعد وفاة والديه، سمع في الكنيسة إنجيل الشاب الغني، فوزّع أملاكه على الفقراء وانطلق إلى حياة الوحدة."
            },

            new SiteContent
            {
                Key = "page.text.38",
                Label = "نص الصفحة 38",
                Section = "fathers",
                Value = "عاش نحو عشرين عامًا في قلعة مهجورة بمنطقة بسبار بمحافظة البحر الأحمر، ثم خرج ليرشد تلاميذه الذين تزاحموا عليه. اشتهر بكتابه «المقالات» وبالمحاورات التي نُقلت عنه في سيرة قد كتبها القديس أثناسيوس."
            },

            new SiteContent
            {
                Key = "page.text.39",
                Label = "نص الصفحة 39",
                Section = "fathers",
                Value = "أثره"
            },

            new SiteContent
            {
                Key = "page.text.40",
                Label = "نص الصفحة 40",
                Section = "fathers",
                Value = "القديس أثناسيوس الرسولي"
            },

            new SiteContent
            {
                Key = "page.text.41",
                Label = "نص الصفحة 41",
                Section = "fathers",
                Value = "بطريرك الإسكندرية العشرون. دافع عن قانون الإيمان في نيقية، ونُفي خمس مرات في سبيل الحقيقة"
            },

            new SiteContent
            {
                Key = "page.text.42",
                Label = "نص الصفحة 42",
                Section = "fathers",
                Value = "وُلد في الإسكندرية نحو عام 296م، وتربّى في كنف الكنيسة حتى صار سكرتيرًا للبطريرك ألكسندروس، ورافقه إلى مجمع نيقية عام 325م شمّاسًا شابًا."
            },

            new SiteContent
            {
                Key = "page.text.43",
                Label = "نص الصفحة 43",
                Section = "fathers",
                Value = "خلف ألكسندروس على الكرسي الإسكندري عام 328م، فوجد نفسه في مواجهة الآريوسيين ومدافعيهم من الأباطرة. نُفي خمس مرات: إلى ترير في ألمانيا، وإلى رومية، وإلى صحراء مصر. وحين مات، كان قد رسّخ الإيمان النيقاوي في الكنيسة كلها."
            },

            new SiteContent
            {
                Key = "page.text.44",
                Label = "نص الصفحة 44",
                Section = "fathers",
                Value = "من مؤلفاته"
            },

            new SiteContent
            {
                Key = "page.text.45",
                Label = "نص الصفحة 45",
                Section = "fathers",
                Value = "القديس كيرلس الكبير"
            },

            new SiteContent
            {
                Key = "page.text.46",
                Label = "نص الصفحة 46",
                Section = "fathers",
                Value = "بطريرك الإسكندرية الرابع والعشرون. صاغ التعبير اللاهوتي الذي حفظته الكنيسة القبطية حتى اليوم"
            },

            new SiteContent
            {
                Key = "page.text.47",
                Label = "نص الصفحة 47",
                Section = "fathers",
                Value = "وُلد في إسكندرية أو في الشرق نحو عام 376م، وتلقّى تعليمًا واسعًا في اللاهوت واللغة اليونانية. صار بطريركًا للإسكندرية عام 412م."
            },

            new SiteContent
            {
                Key = "page.text.48",
                Label = "نص الصفحة 48",
                Section = "fathers",
                Value = "دخل في مواجهة مع نسطور بطريرك القسطنطينية حول لقب «والدة الإله» للعذراء مريم، وانعقد مجمع أفسس عام 431م الذي هزم النسطورية. وقد حفظت الكنيسة القبطية صيغته اللاهوتية كأساس لعقيدتها."
            },

            new SiteContent
            {
                Key = "page.text.49",
                Label = "نص الصفحة 49",
                Section = "fathers",
                Value = "أعماله الباقية"
            },

            new SiteContent
            {
                Key = "page.text.50",
                Label = "نص الصفحة 50",
                Section = "fathers",
                Value = "القديس مقاريوس الكبير"
            },

            new SiteContent
            {
                Key = "page.text.51",
                Label = "نص الصفحة 51",
                Section = "fathers",
                Value = "مؤسس رهبنة برية شيهيت (وادي النطرون)، وأحد أعمدة الأدب الرهباني القبطي"
            },

            new SiteContent
            {
                Key = "page.text.52",
                Label = "نص الصفحة 52",
                Section = "fathers",
                Value = "وُلد في قرية جبل نتريه (وادي النطرون) نحو عام 300م، وتتلمذ على يد القديس أنطونيوس الكبير، حتى قيل إن أنطونيوس قال عنه: «ما تعبت في إخوتي كما تعبت في هذا الذي هو مصري.»"
            },

            new SiteContent
            {
                Key = "page.text.53",
                Label = "نص الصفحة 53",
                Section = "fathers",
                Value = "أسّس رهبنة برية شيهيت، التي صارت أكبر تجمّع رهباني في العالم المسيحي القديم، وأنجبت آباءً كبارًا مثل الأنبا مقاريوس الإسكندري والأنبا بيشوي."
            },

            new SiteContent
            {
                Key = "page.text.54",
                Label = "نص الصفحة 54",
                Section = "fathers",
                Value = "من تراثه"
            },

            new SiteContent
            {
                Key = "page.text.55",
                Label = "نص الصفحة 55",
                Section = "fathers",
                Value = "الأنبا بيشوي"
            },

            new SiteContent
            {
                Key = "page.text.56",
                Label = "نص الصفحة 56",
                Section = "fathers",
                Value = "من كبار آباء برية شيهيت، وله سيرة روحية من أوسع ما وصلنا من الأدب الرهباني القبطي"
            },

            new SiteContent
            {
                Key = "page.text.57",
                Label = "نص الصفحة 57",
                Section = "fathers",
                Value = "وُلد في مصر السفلى نحو عام 320م، وترهّب في برية شيهيت تحت إرشاد الأنبا مقاريوس وأنبا بجيمي. عاش قرب أنبا يوحنا القصير، وصار من أشهر آباء البرية."
            },

            new SiteContent
            {
                Key = "page.text.58",
                Label = "نص الصفحة 58",
                Section = "fathers",
                Value = "صارت «سيرة الأنبا بيشوي» من أشهر النصوص القبطية، وتُرجمت إلى العربية والإثيوبية، وتُقرأ في الكنائس حتى اليوم في المناسبات."
            },

            new SiteContent
            {
                Key = "page.text.59",
                Label = "نص الصفحة 59",
                Section = "fathers",
                Value = "من تراثه"
            },

            new SiteContent
            {
                Key = "page.text.60",
                Label = "نص الصفحة 60",
                Section = "fathers",
                Value = "القديس شنودة رئيس المتوحدين"
            },

            new SiteContent
            {
                Key = "page.text.61",
                Label = "نص الصفحة 61",
                Section = "fathers",
                Value = "أعظم منظّم للرهبنة القبطية، وكاتب باللغة القبطية ترك تراثًا ضخمًا ما زال موضع دراسةٍ عالمية"
            },

            new SiteContent
            {
                Key = "page.text.62",
                Label = "نص الصفحة 62",
                Section = "fathers",
                Value = "وُلد في قرية شنشيف (المنيا) نحو عام 348م، وترهّب في دير أبي حور ببرية سكيت، ثم صار رئيسًا لدير الأنبا شنودة الأبيض بصعيد مصر."
            },

            new SiteContent
            {
                Key = "page.text.63",
                Label = "نص الصفحة 63",
                Section = "fathers",
                Value = "جمع حوله آلاف الرهبان، ووضع لهم نظامًا دقيقًا للعمل والصلاة، وربط الرهبنة بالخدمة والدفاع عن الفقراء والمظلومين حتى أمام الولاة. كُتبه القبطية من أهم مصادر اللغة القبطية."
            },

            new SiteContent
            {
                Key = "page.text.64",
                Label = "نص الصفحة 64",
                Section = "fathers",
                Value = "من تراثه"
            },

            new SiteContent
            {
                Key = "page.text.65",
                Label = "نص الصفحة 65",
                Section = "fathers",
                Value = "الأنبا أبرام أسقف الفيوم"
            },

            new SiteContent
            {
                Key = "page.text.66",
                Label = "نص الصفحة 66",
                Section = "fathers",
                Value = "أسقف الفيوم والجيزة، اشتهر بحنوّه على الفقراء وقربه من البسطاء، وصار أيقونةً للمحبة العملية"
            },

            new SiteContent
            {
                Key = "page.text.67",
                Label = "نص الصفحة 67",
                Section = "fathers",
                Value = "وُلد في قرية بني عدي بمنفلوط عام 1829م، وترهّب في دير السريان ثم في دير البراموس. اختير أسقفًا للفيوم والجيزة عام 1881م، فصار قريبًا من الناس في أفراحهم وأحزانهم."
            },

            new SiteContent
            {
                Key = "page.text.68",
                Label = "نص الصفحة 68",
                Section = "fathers",
                Value = "عُرف بتواضعه الشديد وبمواقفه في نصرة الفقراء والأرامل، وبمواقفه الوطنية في عصره، حتى ذكره معاصروه كواحد من أكثر الشخصيات القبطية تأثيرًا في الريف المصري."
            },

            new SiteContent
            {
                Key = "page.text.69",
                Label = "نص الصفحة 69",
                Section = "fathers",
                Value = "محطات في سيرته"
            },

            new SiteContent
            {
                Key = "page.text.70",
                Label = "نص الصفحة 70",
                Section = "glossary",
                Value = "كلماتٌ نسمعها كل يوم… فماذا تعني؟"
            },

            new SiteContent
            {
                Key = "page.text.71",
                Label = "نص الصفحة 71",
                Section = "glossary",
                Value = "معجم مُبسّط للمصطلحات الطقسية والليتورجية القبطية، لمن يخطو أولى خطواته في فهم الطقس الكنسي."
            },

            new SiteContent
            {
                Key = "page.text.72",
                Label = "نص الصفحة 72",
                Section = "glossary",
                Value = "عدد المصطلحات المعروضة: 14"
            },

            new SiteContent
            {
                Key = "page.text.73",
                Label = "نص الصفحة 73",
                Section = "glossary",
                Value = "القداس الإلهي"
            },

            new SiteContent
            {
                Key = "page.text.74",
                Label = "نص الصفحة 74",
                Section = "glossary",
                Value = "الصلاة الجامعة الرئيسية في الكنيسة، التي يُقدَّم فيها الخبز والخمر ويُتناول المؤمنون. يتكوّن من قسمين: صلاة الشكر (القرابين)، وقداس المؤمنين."
            },

            new SiteContent
            {
                Key = "page.text.75",
                Label = "نص الصفحة 75",
                Section = "glossary",
                Value = "الطقس"
            },

            new SiteContent
            {
                Key = "page.text.76",
                Label = "نص الصفحة 76",
                Section = "glossary",
                Value = "مجموع القواعد والترتيبات التي تُمارَس بها الصلوات والأسرار في الكنيسة. الطقس القبطي هو طقس الإسكندرية القديم، ويتميّز بصلواته الطويلة ولحنه الخاص."
            },

            new SiteContent
            {
                Key = "page.text.77",
                Label = "نص الصفحة 77",
                Section = "glossary",
                Value = "الأجبية"
            },

            new SiteContent
            {
                Key = "page.text.78",
                Label = "نص الصفحة 78",
                Section = "glossary",
                Value = "كتاب «السواعي» أو الصلوات السبع اليومية التي تُصلَّى في أوقات محددة من اليوم، مستمدّة من صلاة السواعي في العهد القديم وممارسة الكنيسة الأولى."
            },

            new SiteContent
            {
                Key = "page.text.79",
                Label = "نص الصفحة 79",
                Section = "glossary",
                Value = "القطمارس"
            },

            new SiteContent
            {
                Key = "page.text.80",
                Label = "نص الصفحة 80",
                Section = "glossary",
                Value = "كتاب ترتيب القراءات الكتابية على مدار السنة الطقسية القبطية، بحيث يكون لكل يوم قراءة معيّنة من الرسائل والأناجيل تتناسب مع المناسبة."
            },

            new SiteContent
            {
                Key = "page.text.81",
                Label = "نص الصفحة 81",
                Section = "glossary",
                Value = "السنكسار"
            },

            new SiteContent
            {
                Key = "page.text.82",
                Label = "نص الصفحة 82",
                Section = "glossary",
                Value = "كتاب يشتمل على سير القديسين والشهداء مرتّبة حسب أيام السنة القبطية، ويُقرأ عادةً بعد رفع بخور عشية أو بعد قراءة الإنجيل في القداس."
            },

            new SiteContent
            {
                Key = "page.text.83",
                Label = "نص الصفحة 83",
                Section = "glossary",
                Value = "الدفنار"
            },

            new SiteContent
            {
                Key = "page.text.84",
                Label = "نص الصفحة 84",
                Section = "glossary",
                Value = "مجموعة من التسابيح والألحان والمدائح تُقال للعذراء مريم وللقديسين، وتُصلَّى عادةً في العشيات وطقس التسبحة، وتُرتَّل بلحن خاص."
            },

            new SiteContent
            {
                Key = "page.text.85",
                Label = "نص الصفحة 85",
                Section = "glossary",
                Value = "الإبصلمودية"
            },

            new SiteContent
            {
                Key = "page.text.86",
                Label = "نص الصفحة 86",
                Section = "glossary",
                Value = "كتاب يحتوي على ألحان التسبحة اليومية، كُتب بلغات مختلفة، ويُرتَّل في طقس التسبحة مساءً وصباحًا في الكنيسة القبطية."
            },

            new SiteContent
            {
                Key = "page.text.87",
                Label = "نص الصفحة 87",
                Section = "glossary",
                Value = "اللحن القبطي"
            },

            new SiteContent
            {
                Key = "page.text.88",
                Label = "نص الصفحة 88",
                Section = "glossary",
                Value = "النغمة الموسيقية الخاصة بالكنيسة القبطية، وتُقسَّم إلى أنواع مثل القبطي والفرايحي والشامي والصعيدي، ولكل نوع استخدام طقسي محدّد."
            },

            new SiteContent
            {
                Key = "page.text.89",
                Label = "نص الصفحة 89",
                Section = "glossary",
                Value = "الصنعة"
            },

            new SiteContent
            {
                Key = "page.text.90",
                Label = "نص الصفحة 90",
                Section = "glossary",
                Value = "قِطعة قماشية مستطيلة تُلبَس على الكتف، يلبسها الشماس أثناء الخدمة، وتُعدّ رمزًا للثبات والخدمة في الخورس."
            },

            new SiteContent
            {
                Key = "page.text.91",
                Label = "نص الصفحة 91",
                Section = "glossary",
                Value = "الميرون"
            },

            new SiteContent
            {
                Key = "page.text.92",
                Label = "نص الصفحة 92",
                Section = "glossary",
                Value = "زيت مقدّس يُصنع من خليط كبير من المواد العطرية، ويُستخدم في سرّ الميرون (التثبيت) لتثبيت المؤمن في الروح القدس. يُعدّ تقديسه من أهم المناسبات الكنسية."
            },

            new SiteContent
            {
                Key = "page.text.93",
                Label = "نص الصفحة 93",
                Section = "glossary",
                Value = "التناول"
            },

            new SiteContent
            {
                Key = "page.text.94",
                Label = "نص الصفحة 94",
                Section = "glossary",
                Value = "تناوُل جسد المسيح ودمه تحت صورتي الخبز والخمر، وهي ذروة القداس الإلهي، ويُعدّ المؤمن نفسه لها بالصوم والاعتراف والمصالحة."
            },

            new SiteContent
            {
                Key = "page.text.95",
                Label = "نص الصفحة 95",
                Section = "glossary",
                Value = "تقويم الشهداء"
            },

            new SiteContent
            {
                Key = "page.text.96",
                Label = "نص الصفحة 96",
                Section = "glossary",
                Value = "التقويم القبطي الذي يبدأ من عام 284م، سنة اعتلاء الإمبراطور دقلديانوس العرش وبداية عصر الاضطهاد الكبير. يُستخدم اليوم في تحديد الأعياد والأصوام."
            },

            new SiteContent
            {
                Key = "page.text.97",
                Label = "نص الصفحة 97",
                Section = "glossary",
                Value = "الكرسي الإسكندري"
            },

            new SiteContent
            {
                Key = "page.text.98",
                Label = "نص الصفحة 98",
                Section = "glossary",
                Value = "الكرسي الرسولي الذي أسّسه مارمرقس في الإسكندرية، ويُلقّب بطريره بـ«بابا الإسكندرية». وهو أحد أقدم الكراسي في المسيحية كلها."
            },

            new SiteContent
            {
                Key = "page.text.99",
                Label = "نص الصفحة 99",
                Section = "glossary",
                Value = "الإكليريكية"
            },

            new SiteContent
            {
                Key = "page.text.100",
                Label = "نص الصفحة 100",
                Section = "glossary",
                Value = "الكلية اللاهوتية القبطية الأرثوذكسية، تأسست بالقاهرة عام 1893 لتعليم اللاهوت واللغات القبطية واليونانية، وكانت نواة النهضة التعليمية الحديثة."
            },

            new SiteContent
            {
                Key = "page.text.101",
                Label = "نص الصفحة 101",
                Section = "glossary",
                Value = "جرّب كلمة أخرى، أو تصفّح التصنيفات بالأعلى."
            },

            new SiteContent
            {
                Key = "page.text.102",
                Label = "نص الصفحة 102",
                Section = "articles",
                Value = "ما لا تسعه الحلقة… يسعه القلم"
            },

            new SiteContent
            {
                Key = "page.text.103",
                Label = "نص الصفحة 103",
                Section = "articles",
                Value = "مقالات مطوّلة تتوسّع في مواضيع الحلقات، بمراجع وتفاصيل إضافية لمن يريد أن يتعمّق أكثر."
            },

            new SiteContent
            {
                Key = "page.text.104",
                Label = "نص الصفحة 104",
                Section = "articles",
                Value = "لماذا يهمس التاريخ في أذن الأقباط؟"
            },

            new SiteContent
            {
                Key = "page.text.105",
                Label = "نص الصفحة 105",
                Section = "articles",
                Value = "عن العلاقة الملتبسة بين الذاكرة الجماعية والهوية المعاصرة، ولماذا يصبح حفظ التاريخ مسؤولية وجودية لا ترفًا فكريًّا."
            },

            new SiteContent
            {
                Key = "page.text.106",
                Label = "نص الصفحة 106",
                Section = "articles",
                Value = "لا يتذكّر الأقباط التاريخ لأنهم يحبّون الماضي، بل لأن الحاضر نفسه لا يستقيم دون إجابة عن سؤال: من نحن؟ ومن أين جئنا؟ في المجتمعات التي تمرّ بتحوّلات سريعة، تتحوّل الذاكرة من ترفٍ ثقافي إلى ضرورةٍ وجودية."
            },

            new SiteContent
            {
                Key = "page.text.107",
                Label = "نص الصفحة 107",
                Section = "articles",
                Value = "التاريخ القبطي ليس سلسلة تواريخ وأسماء فقط؛ إنه منظومة قيم: الصمود، والثبات، والانتماء الذي لا يتغيّر بتغيّر الظروف. ولهذا فإن قراءة هذا التاريخ بشكل منظّم ليست عملًا بحثيًّا محضًا، بل فعلٌ في بناء الهوية نفسها."
            },

            new SiteContent
            {
                Key = "page.text.108",
                Label = "نص الصفحة 108",
                Section = "articles",
                Value = "ثلاثة أسئلة يطرحها التاريخ على الحاضر"
            },

            new SiteContent
            {
                Key = "page.text.109",
                Label = "نص الصفحة 109",
                Section = "articles",
                Value = "هذه الأسئلة هي بالضبط الإطار الذي تعمل من خلاله كوبتيفاي: توثيقٌ يسبق التأويل، ومعلومةٌ تسند العاطفة."
            },

            new SiteContent
            {
                Key = "page.text.110",
                Label = "نص الصفحة 110",
                Section = "articles",
                Value = "الجذور المصرية القديمة في الطقس القبطي"
            },

            new SiteContent
            {
                Key = "page.text.111",
                Label = "نص الصفحة 111",
                Section = "articles",
                Value = "من استخدام البخور والمزامير إلى تقسيمات الصلاة اليومية، ما الذي ورثه الطقس المسيحي المصري عن أجداده؟"
            },

            new SiteContent
            {
                Key = "page.text.112",
                Label = "نص الصفحة 112",
                Section = "articles",
                Value = "حين دخلت المسيحية إلى مصر، لم تدخل إلى فراغ ثقافي. كان في مصر معابد وطقوس وموسمٌ ديني منظّم، ولذلك فإن كثيرًا من التفاصيل الطقسية التي نراها اليوم تحمل بصماتٍ مصرية قديمة واضحة."
            },

            new SiteContent
            {
                Key = "page.text.113",
                Label = "نص الصفحة 113",
                Section = "articles",
                Value = "البخور، على سبيل المثال، كان جزءًا أساسيًّا من الطقس المصري القديم قبل أن يصبح رمزًا للصلاة في الكنيسة. وانقسام النهار إلى ساعات للصلاة يشبه في بنيته تقسيمات العبادة المصرية القديمة."
            },

            new SiteContent
            {
                Key = "page.text.114",
                Label = "نص الصفحة 114",
                Section = "articles",
                Value = "ملامح التلاقي"
            },

            new SiteContent
            {
                Key = "page.text.115",
                Label = "نص الصفحة 115",
                Section = "articles",
                Value = "المهم هنا ألا نقرأ هذا التلاقي كتشابه عرضي، بل كدليلٍ على أن المسيحية المصرية نبتت في تربةٍ محلية، وليس في هواءٍ مستعار."
            },

            new SiteContent
            {
                Key = "page.text.116",
                Label = "نص الصفحة 116",
                Section = "articles",
                Value = "مدرسة الإسكندرية: أول جامعة في التاريخ؟"
            },

            new SiteContent
            {
                Key = "page.text.117",
                Label = "نص الصفحة 117",
                Section = "articles",
                Value = "كيف صارت مدرسة لاهوتية في مدينة ساحلية مؤسسةً فكرية غيّرت مسار الفلسفة واللاهوت في العالم كله."
            },

            new SiteContent
            {
                Key = "page.text.118",
                Label = "نص الصفحة 118",
                Section = "articles",
                Value = "ليست «الجامعة» مفهومًا حديثًا فقط؛ فمدرسة الإسكندرية قدّمت نموذجًا مبكّرًا لمؤسسة تعليمية منظّمة، بمنهجٍ ومعلّمين وتلاميذ ومكتبة، وأثرٍ يمتدّ عبر القرون."
            },

            new SiteContent
            {
                Key = "page.text.119",
                Label = "نص الصفحة 119",
                Section = "articles",
                Value = "بدأت المدرسة كمدرسة تعليم ديني للموعوظين، ثم تطوّرت على يد بانتينوس وكليمندس الإسكندري وأوريجانوس إلى مؤسسة للبحث اللاهوتي والفلسفي، تجمع بين العقل والنص."
            },

            new SiteContent
            {
                Key = "page.text.120",
                Label = "نص الصفحة 120",
                Section = "articles",
                Value = "ثلاثة أسماء صنعت المدرسة"
            },

            new SiteContent
            {
                Key = "page.text.121",
                Label = "نص الصفحة 121",
                Section = "articles",
                Value = "أثر هذه المدرسة لم يتوقف عند حدود مصر؛ فقد امتدّ إلى القسطنطينية وروما وسائر الشرق، وصار منهجها في الحوار العقلي أساسًا للاهوت المسيحي كله."
            },

            new SiteContent
            {
                Key = "page.text.122",
                Label = "نص الصفحة 122",
                Section = "articles",
                Value = "اللغة القبطية: من الهيروغليفية إلى الليتورجيا"
            },

            new SiteContent
            {
                Key = "page.text.123",
                Label = "نص الصفحة 123",
                Section = "articles",
                Value = "كيف وُلدت آخر مراحل اللغة المصرية، ولماذا أصبحت اليوم جسرًا بين مصر القديمة ومصر المسيحية؟"
            },

            new SiteContent
            {
                Key = "page.text.124",
                Label = "نص الصفحة 124",
                Section = "articles",
                Value = "اللغة القبطية ليست لغةً جديدة، بل هي المرحلة الأخيرة من اللغة المصرية القديمة، كُتبت بالحروف اليونانية مضافًا إليها سبعة أحرف مأخوذة من الديموطيقية لتمثيل أصواتٍ لا توجد في اليونانية."
            },

            new SiteContent
            {
                Key = "page.text.125",
                Label = "نص الصفحة 125",
                Section = "articles",
                Value = "انتشرت القبطية بسرعة لأنها سهّلت القراءة والكتابة على المصريين، وصارت لغة الترجمة واللاهوت والليتورجيا. وحين تراجعت لغةً يومية، بقيت لغة صلاة."
            },

            new SiteContent
            {
                Key = "page.text.126",
                Label = "نص الصفحة 126",
                Section = "articles",
                Value = "لماذا بقيت؟"
            },

            new SiteContent
            {
                Key = "page.text.127",
                Label = "نص الصفحة 127",
                Section = "articles",
                Value = "الرهبنة المصرية وأثرها على العالم المسيحي"
            },

            new SiteContent
            {
                Key = "page.text.128",
                Label = "نص الصفحة 128",
                Section = "articles",
                Value = "من مغارات البحر الأحمر إلى أديرة أوروبا: كيف صدرت مصر نموذجًا روحانيًّا للعالم كله؟"
            },

            new SiteContent
            {
                Key = "page.text.129",
                Label = "نص الصفحة 129",
                Section = "articles",
                Value = "حين رحل القديس أنطونيوس إلى الصحراء، لم يكن يعلم أنه يؤسّس حركة ستغيّر تاريخ المسيحية كلها. فالرهبنة التي وُلدت في مصر، انتقلت إلى فلسطين وسوريا وآسيا الصغرى ثم إلى أوروبا الغربية."
            },

            new SiteContent
            {
                Key = "page.text.130",
                Label = "نص الصفحة 130",
                Section = "articles",
                Value = "وقد كان لكتاب «حياة أنطونيوس» لأثناسيوس أثرٌ بالغ؛ فبعد ترجمته إلى اللاتينية، تحمّس كثيرون لحياة الرهبنة، وكان من بينهم أوغسطينوس نفسه."
            },

            new SiteContent
            {
                Key = "page.text.131",
                Label = "نص الصفحة 131",
                Section = "articles",
                Value = "ثلاثة نماذج"
            },

            new SiteContent
            {
                Key = "page.text.132",
                Label = "نص الصفحة 132",
                Section = "articles",
                Value = "كيف نقرأ السنكسار؟ دليل مبتدئ"
            },

            new SiteContent
            {
                Key = "page.text.133",
                Label = "نص الصفحة 133",
                Section = "articles",
                Value = "تعرّف على بنية السنكسار، وكيف تقرأ سيرة قديس في سياقها التاريخي دون أن تفقد البُعد الروحي."
            },

            new SiteContent
            {
                Key = "page.text.134",
                Label = "نص الصفحة 134",
                Section = "articles",
                Value = "السنكسار ليس كتاب تاريخ بالمعنى الأكاديمي، بل كتاب كنسي روحي يربط المؤمن بقديس اليوم. ومع ذلك، يمكن قراءته بعينٍ نقدية تحترم السياق وتحفظ البُعد الروحي."
            },

            new SiteContent
            {
                Key = "page.text.135",
                Label = "نص الصفحة 135",
                Section = "articles",
                Value = "نبدأ بمعرفة التاريخ القبطي المقابل لليوم الميلادي، ثم نبحث عن السيرة، ثم ننتبه إلى تفاصيل المكان والزمان، وأخيرًا نقرأ الرسالة العملية التي توضع في نهاية السيرة."
            },

            new SiteContent
            {
                Key = "page.text.136",
                Label = "نص الصفحة 136",
                Section = "about",
                Value = "«الشعب الذي لا يعرف تاريخه، لا يستطيع أن يكتب مستقبله.»"
            },

            new SiteContent
            {
                Key = "page.text.137",
                Label = "نص الصفحة 137",
                Section = "about",
                Value = "منصةٌ وُلدت من سؤالٍ بسيط"
            },

            new SiteContent
            {
                Key = "page.text.138",
                Label = "نص الصفحة 138",
                Section = "about",
                Value = "كوبتيفاي بدأت بسؤالٍ يتكرر على ألسنة الشباب القبطي: «من أين أبدأ؟». لم يكن هناك مكانٌ واحد يجمع التاريخ والطقس والألحان والآباء والمصطلحات بلغة عربية معاصرة يمكن فهمها دون خلفية أكاديمية."
            },

            new SiteContent
            {
                Key = "page.text.139",
                Label = "نص الصفحة 139",
                Section = "about",
                Value = "لذلك أنشأنا منصة توثيقية تجمع المعرفة الكنسية وتُوصّلها بصورة واضحة، معتمدة على المصادر الكنسية والأدبيات المعتمدة، وبروحٍ تحترم قداسة الموضوع دون أن تتعقّده."
            },

            new SiteContent
            {
                Key = "page.text.140",
                Label = "نص الصفحة 140",
                Section = "about",
                Value = "كل معلومة تُراجَع من مصادر كنسية وتاريخية معتمدة قبل النشر، ونُعلن مراجعنا دائمًا."
            },

            new SiteContent
            {
                Key = "page.text.141",
                Label = "نص الصفحة 141",
                Section = "about",
                Value = "نشرح العميق بلغة واضحة، دون أن نفقد دقّة المضمون أو أمانة التعبير."
            },

            new SiteContent
            {
                Key = "page.text.142",
                Label = "نص الصفحة 142",
                Section = "about",
                Value = "نبني أرشيفًا رقميًّا يبقى بعدنا: فيديو، ومقالات، ومعجم، وخط زمني قابل للتوسّع."
            },

            new SiteContent
            {
                Key = "page.text.143",
                Label = "نص الصفحة 143",
                Section = "faq",
                Value = "أسئلة يسألها الجميع"
            },

            new SiteContent
            {
                Key = "page.text.144",
                Label = "نص الصفحة 144",
                Section = "faq",
                Value = "جمعنا أكثر ما يُسأل عنه فريق كوبتيفاي. إن لم تجد سؤالك، راسلنا على البريد بالأسفل."
            },

            new SiteContent
            {
                Key = "page.text.145",
                Label = "نص الصفحة 145",
                Section = "faq",
                Value = "كوبتيفاي منصة عربية متخصصة في التاريخ والتراث والهوية القبطية الأرثوذكسية. تقدّم حلقات مرئية أسبوعية، وخطًّا زمنيًّا تفاعليًّا، ودليلًا لآباء الكنيسة، ومعجمًا للمصطلحات الطقسية، ومقالاتٍ معمّقة، بهدف توثيق الذاكرة الكنسية بلغة معاصرة مفهومة."
            },

            new SiteContent
            {
                Key = "page.text.146",
                Label = "نص الصفحة 146",
                Section = "faq",
                Value = "نعم. صُمّمت المنصة لتكون مفهومة لأي قارئ عربي مهتم بتاريخ مصر وثقافتها، دون افتراض خلفية دينية مسبقة. ونحرص على شرح المصطلحات الطقسية عند أول استخدامها، مع معجم كامل للرجوع إليه."
            },

            new SiteContent
            {
                Key = "page.text.147",
                Label = "نص الصفحة 147",
                Section = "faq",
                Value = "تُقدَّم الحلقات بالعربية الفصحى المُيسَّرة، مع الحفاظ على المصطلحات القبطية والطقسية بنطقها الصحيح. ونعمل على إضافة ترجمات نصية للحلقات لتسهيل القراءة والبحث."
            },

            new SiteContent
            {
                Key = "page.text.148",
                Label = "نص الصفحة 148",
                Section = "faq",
                Value = "يمكنك إرسال اقتراحك مباشرة إلى بريد المنصة coptify.page@gmail.com ، أو عبر رسالة خاصة على صفحتنا على فيسبوك أو إنستجرام. نقرأ كل الاقتراحات، ونُدرج الأكثر تكرارًا في خطة الحلقات الشهرية."
            },

            new SiteContent
            {
                Key = "page.text.149",
                Label = "نص الصفحة 149",
                Section = "faq",
                Value = "نشجّع مشاركة الحلقات والمقالات عبر الروابط المباشرة أو إعادة نشر الفيديو من صفحاتنا الرسمية. أما إعادة نشر النصوص كاملةً فيُرجى مراسلتنا للحصول على إذن مكتوب، حفاظًا على جودة التوثيق ودقّته."
            },

            new SiteContent
            {
                Key = "page.text.150",
                Label = "نص الصفحة 150",
                Section = "faq",
                Value = "كوبتيفاي مبادرة مستقلة غير رسمية، تعمل بمشاركة باحثين ومتخصصين في التاريخ الكنسي والطقس القبطي، وبمراجعة من آباء وخدام ذوي خبرة. ونرحّب بأي ملاحظة تصحيحية تُسهم في دقّة المحتوى."
            },

            new SiteContent
            {
                Key = "page.text.151",
                Label = "نص الصفحة 151",
                Section = "newsletter",
                Value = "لا تفوّت حلقة الجمعة"
            },

            new SiteContent
            {
                Key = "page.text.152",
                Label = "نص الصفحة 152",
                Section = "newsletter",
                Value = "رسالة واحدة كل أسبوع: الحلقة الجديدة، مقال مختار، ومصطلح من المعجم مع شرحه. بلا إعلانات، وبلا إزعاج."
            },

            new SiteContent
            {
                Key = "page.text.153",
                Label = "نص الصفحة 153",
                Section = "newsletter",
                Value = "بالاشتراك أنت توافق على تلقّي رسائل بريدية من كوبتيفاي. يمكنك إلغاء الاشتراك في أي وقت."
            }
        });
    }

    db.SaveChanges();
}