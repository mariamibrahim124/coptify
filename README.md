# Coptify — Production-ready Admin CMS

منصة كوبتيفاي مبنية بـ ASP.NET Core 8 + SQLite + HTML/CSS/JS.

## أهم ما في النسخة

- الموقع العام هو نفسه الذي يراه الزائر.
- `/admin` يفتح محرر الموقع المرئي.
- Admin Login حقيقي باستخدام Cookie Authentication.
- Password محفوظة كـ PBKDF2 hash + salt، وليست plaintext.
- تغيير Username وPassword من زر **الحساب** داخل لوحة الإدارة.
- تعديل النصوص من نفس الصفحة: اضغطي على أي نص عليه إطار التحرير، عدّلي ثم احفظي.
- كل النصوص الموجودة في الصفحة، بما فيها الخط الزمني والمعجم والمقالات والأسئلة والفوتر، مربوطة بمفاتيح CMS.
- إدارة الفيديوهات من نفس الصفحة: **إضافة فيديو / تعديل فيديو / حذف فيديو**.
- الفيديوهات تُحفظ كروابط Facebook فقط؛ لا يتم رفع ملفات الفيديو للسيرفر.
- يمكن حفظ Thumbnail كرابط صورة خارجي.

## تشغيل محليًا

يتطلب .NET 8 SDK.

```bash
dotnet restore
dotnet run
```

ثم:

```text
http://localhost:5005/
http://localhost:5005/admin
```

### أول دخول محلي

إذا لم تضعي Environment Variables، يتم إنشاء Admin أول مرة بالبيانات:

```text
Username: admin
Password: ChangeMe123!
```

بعد أول دخول غيّري كلمة المرور من **⚙ الحساب**.

## Production

قبل النشر اضبطي:

```text
COPTIFY_ADMIN_USERNAME=اسم_المستخدم
COPTIFY_ADMIN_PASSWORD=كلمة_مرور_قوية_جديدة
```

لا تضعي كلمة المرور في GitHub.

SQLite مناسب كبداية، لكن لو كان الموقع سيستقبل تعديلات كثيرة أو سيعمل على أكثر من instance يفضل نقل قاعدة البيانات إلى PostgreSQL/SQL Server managed.

## GitHub

لا ترفعي:

- `coptify.db`
- `.env`
- أي API keys أو secrets

ملف `.gitignore` يتجاهل قاعدة SQLite وملفات build.
