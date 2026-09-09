# תזכורות מתוזמנות (Scheduled Reminders)

משימת בית מלאה (full-stack): תזכורות מתוזמנות עם אימות JWT, הרשאות לפי תפקיד, תהליך רקע שמבצע שליחה מדומה, וממשק Angular.

## תחילת עבודה

### דרישות מקדימות

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (לאפליקציית Angular)

### הרצת ה-backend

```bash
cd backend
dotnet restore
dotnet run
```

ה-API מאזין בכתובת **http://localhost:5288**.

### הרצת בדיקות

משורש הריפוזיטורי:

```bash
dotnet test
```

הסוויטה בודקת יצירה/תפיסה/השלמה של תזכורות ותפקידי התחברות (`tests/ScheduledReminders.Api.Tests`). הרשאות HTTP של Admin מול Viewer נאכפות בנקודות הקצה של ה-API ואינן מכוסות כאן, כדי לא לשנות את `Program.cs` של הייצור לצורך `WebApplicationFactory`.

### הרצת אפליקציית Angular

בטרמינל שני:

```bash
cd frontend
npm install
npm start
```

הממשק מאזין בכתובת **http://localhost:4200** ופונה ל-API ב-`http://localhost:5288`. CORS מורשה ל-`http://localhost:4200` ול-`http://127.0.0.1:4200`.

קוד הפרונט נמצא בתיקייה `frontend/` בשורש הריפוזיטורי (Angular 18: מסכי התחברות, רשימה, יצירה/עריכה ופירוט עם היסטוריית ביצוע).

### מסד נתונים In-Memory

ה-API משתמש ב-**EF Core In-Memory**. הנתונים חיים רק בתהליך הרץ:

- משתמשים מאותחלים נוצרים בעלייה.
- תזכורות והיסטוריית ביצוע קיימות רק כל עוד התהליך רץ.
- הפעלה מחדש של ה-API מאפסת תזכורות והיסטוריה (המשתמשים מאותחלים מחדש).

אין צורך ב-SQL Server, Docker או מחרוזת חיבור.

### איך להריץ / לבדוק

1. להפעיל את ה-backend ואז את אפליקציית Angular.
2. להתחבר כ-Admin או כ-Viewer.
3. כ-Admin, ליצור תזכורת שמתוזמנת כמה שניות בעבר (`IsActive = true`, `FutureRunsCount >= 1`). הסטטוס מתחיל כ-**Pending**.
4. תוך כשנייה היא אמורה לעבור ל-**Running** למשך כ-10 שניות, ואז ל-**Success** או **Failed**.
5. לפתוח את מסך הפירוט (או `GET /api/reminders/{id}/executions`) להיסטוריה, מהחדש לישן.

בדיקה דרך ה-API בלבד: http://localhost:5288/swagger — להתחבר, ללחוץ **Authorize**, ולהדביק את ה-JWT.

### חשבונות מאותחלים

| תפקיד | שם משתמש | סיסמה |
|--------|----------|-------------|
| Admin  | `admin`  | `Admin123!` |
| Viewer | `viewer` | `Viewer123!` |

### כתובת Swagger

http://localhost:5288/swagger

### הגדרות

הגדרות JWT נמצאות תחת הסעיף `Jwt` ב-`backend/appsettings.json`:

- `Issuer`
- `Audience`
- `ExpiryHours` (8)
- `Key` — מפתח החתימה

אין לקממט סודות ייצור. `appsettings.Development.json` מכיל מפתח חתימה **לפיתוח בלבד**. בסביבת **Development**, אם `Jwt:Key` ריק, היישום נופל חזרה לאותו מפתח פיתוח כדי שהפרויקט ירוץ מיד אחרי clone. בסביבות **שאינן Development** (כולל Production) חובה להגדיר את `Jwt:Key` (למשל דרך `Jwt__Key`). אם המפתח חסר או ריק מחוץ ל-Development, ה-API **נכשל מיד בעלייה** ואינו משתמש במפתח הדמו.

הגדרות `ReminderProcessor`:

- `PollIntervalSeconds` (ברירת מחדל `1`) — מרווח הסריקה בשניות: באיזו תדירות העובד מחפש תזכורות לפירעון.
- `SimulationDelaySeconds` (ברירת מחדל `10`) — משך השליחה המדומה.
- `StaleRunningThresholdSeconds` (ברירת מחדל `30`) — כמה זמן ביצוע בסטטוס `Running` בלי `CompletedAt` חייב להיות ישן לפני ששחזור בעלייה/סקר מסמן אותו כ-Failed. הסף האפקטיבי לעולם אינו קצר ממשך הסימולציה ועוד שנייה אחת, כדי ששליחה פעילה לא תטופל כקריסה.

כתובת הבסיס של ה-API באנגולר היא `frontend/src/environments/environment.ts` (`http://localhost:5288`).

CORS מופעל עבור `http://localhost:4200` ו-`http://127.0.0.1:4200`.

## ארכיטקטורה והחלטות טכנולוגיות

**Minimal API** נבחר כי המשימה מחייבת אותו, ושטח הפנים קטן מספיק כדי שמיפוי נקודות הקצה יישאר קריא בלי controllers.

**EF Core In-Memory** נבחר כדי שמעריכים יוכלו לשכפל ולהריץ את ה-API בלי להתקין מסד נתונים.

**ReminderService** מחזיק כללי יצירה/עדכון/קריאה (כולל `Status = Pending` ביצירה) ושאילתות היסטוריית ביצוע. קבצי ה-endpoints מבצעים bind, ולידציה, הרשאה ומיפוי לתוצאות HTTP.

**JWT** מאמת את לקוח Angular. ה-backend מנפיק ומוודא issuer, audience, תוקף ומפתח חתימה. תביעת התפקיד בטוקן אינה נלקחת מגוף בקשת הלקוח — היא מונפקת בהתחברות מתוך רשומת המשתמש המאותחל.

**הרשאה לפי תפקיד ב-backend** היא גבול האבטחה. GET (כולל היסטוריית ביצוע) מותר ל-Admin ול-Viewer. POST ו-PUT דורשים Admin. טוקן Viewer מקבל **403 Forbidden** בפעולות שמיועדות ל-Admin בלבד. שומרי הנתיב באנגולר מסתירים מסכי יצירה/עריכה כשכבת UX בלבד.

**ניהול מצב ב-Angular.** מצב האימות יושב ב-`AuthService` כ-`BehaviorSubject`, כדי שהמעטפת והנתיבים יגיבו להתחברות ולהתנתקות בלי store גלובלי. הסשן הנוכחי (JWT, שם משתמש, תפקיד, תפוגה) נשמר ב-`localStorage` כדי שרענון יחזיר את המשתמש עד שפג תוקף הטוקן. interceptor של HTTP מוסיף `Authorization: Bearer <token>` לקריאות API כשיש סשן. שומרי נתיב (`authGuard`, `adminGuard`) מסתירים מסכי Admin (יצירה/עריכה); זו שכבת UX בלבד — ה-API עדיין אוכף תפקידים. NgRx וספריות דומות לא בשימוש: מצב האפליקציה קטן (סשן ונתון HTTP לכל מסך), ו-`BehaviorSubject` בשירות שומר על הלקוח קל ועדיין מספק מצב אימות ריאקטיבי.

## עיבוד ברקע

`BackgroundService` בשם `ReminderProcessorHostedService` רץ לאורך חיי תהליך ה-API. זה מתאים כאן: עובד יחיד בתוך התהליך, בלי תשתיות נוספות, ותמיכה מובנית ב-`CancellationToken` לכיבוי.

**סריקה (polling).** בערך כל שנייה נפתח scope של DI, ו-`ReminderExecutionService` מתבקש לתזכורות זכאיות:

- `IsActive == true`
- `Status == Pending`
- `ScheduledAt <= DateTime.UtcNow`
- `FutureRunsCount > 0`

**תפיסה לפני המתנה (claim before wait).** כל תזכורת זכאית נתפסת **ברצף** באותו מחזור: `Pending` → `Running`, נוצרת שורת `ReminderExecution` עם `Status = Running` ו-`StartedAt` (UTC), ו-`SaveChangesAsync` רץ **לפני** תפיסת התזכורת הבאה ו**לפני** כל המתנה של 10 שניות. מכיוון שרק כותב אחד סוקר, התפיסה שנשמרה מספיקה כדי שהתזכורת לא תיבחר שוב כל עוד היא `Running`. אין מנעולים, טוקנים או ברוקרים נוספים. כל מחזור מריץ שחזור ביצועים תקועים **לפני** התפיסה, באותו scope של DI.

**שחזור אחרי קריסה.** אם התהליך מת במהלך ההמתנה המדומה, תזכורת יכולה להישאר `Running` עם ביצוע בלי `CompletedAt`. שורות כאלה מדולגות על ידי מסנן הזכאות הרגיל. השחזור מחפש רק ביצועים שעדיין `Running`, בלי `CompletedAt`, ושה-`StartedAt` שלהם ישן מהסף (UTC). הוא מסמן את הביצוע כ-`Failed` (עם `CompletedAt`), מחזיר את התזכורת ל-`Pending` אם היא עדיין `Running`, ו**אינו** משנה `FutureRunsCount`, `ScheduledAt` או `IsActive`. המחזוריות נשמרת; התפיסה הבאה היא ניסיון חדש. השחזור אידמפוטנטי. נתוני In-Memory עדיין נמחקים ביציאת התהליך; השחזור רלוונטי אם אותו תהליך in-memory משאיר יתומים אחרי חריגה, או אם בעתיד ייעשה שימוש במסד עמיד.

**סימולציה במקביל.** אחרי שכל התפיסות במחזור נשמרו, השליחה המדומה של 10 שניות לכל תזכורת שנתפסה רצה **במקביל** (`Task.WhenAll`). כל משימה יוצרת **scope חדש** של DI דרך `IServiceScopeFactory` ולכן **`DbContext` חדש**. אין שיתוף `DbContext` בין threads.

**מעברי מצב.**

- תפיסה: תזכורת `Pending` → `Running`; ביצוע `Running` עם `StartedAt`.
- אחרי המתנת 10 השניות, התוצאה המדומה היא באקראי `Success` או `Failed`.
- לביצוע נקבעים `CompletedAt` והסטטוס הסופי.
- **Once:** `FutureRunsCount = 0`, `IsActive = false`, סטטוס התזכורת נשאר `Success` או `Failed` (לא חוזר ל-`Pending`). היא לא תרוץ שוב.
- **Daily / Weekly / Monthly:** `FutureRunsCount` יורד ב-1 (גם בכשל). אם נשאר `> 0`, `ScheduledAt` מתקדם ביום / 7 ימים / חודש לוח שנה, והסטטוס חוזר ל-`Pending`. אם מגיעים ל-`0`, אין ריצה הבאה; הסטטוס נשאר `Success` או `Failed` ו-**`IsActive` נקבע ל-`false`** כדי שדגל הפעילות בממשק יתאים ל־«אין ריצות נותרות».

**FutureRunsCount** נצרך רק כשביצוע באמת רץ (תפיסה + שליחה מדומה). הוא אינו נגזר מהתדירות.

**ביטול.** `Task.Delay` משתמש בטוקן העצירה של שירות הרקע. `OperationCanceledException` מכיבוי אינו נתפס ומשוכתב כ-`Failed` באותו רגע. אם התהליך ממשיך לרוץ או עולה מאוחר יותר עם נתונים שנשמרו, ביצוע `Running` ישן מהסף משוחזר כ-Failed והתזכורת חוזרת ל-Pending בלי לצרוך `FutureRunsCount`.

**בידוד.** תפיסת תזכורת והשלמת שליחה מדומה עטופות כך שחריגה לא צפויה של תזכורת אחת נרשמת בלוג ואינה עוצרת את לולאת הסריקה או סימולציות מקבילות אחרות.

**מקביליות וסקייל.** משימת הבית הזו משתמשת ב-EF Core In-Memory ובמעבד hosted **יחיד** בתהליך אחד. תפיסה כפולה אינה צפויה במצב הזה: העובד תופס ברצף, ומצב In-Memory אינו משותף בין תהליכים. פריסת ייצור עם כמה מופעי אפליקציה ומסד משותף ועמיד תדרוש תפיסה אטומית (למשל `UPDATE … WHERE Status = Pending`) ו/או מנעול מבוזר כדי ששני צמתים לא יריצו את אותה תזכורת. זה מחוץ להיקף במכוון. שחזור אחרי קריסה מתקן רק ביצועי `Running` תקועים; הוא **אינו** מנעול מבוזר.

## טיפול בזמן

ה-backend משתמש ב-**UTC** לתיזמון ולשמירה. ערכי `ScheduledAt` נכנסים מנורמלים ל-UTC. הזכאות משווה את `ScheduledAt` ל-`DateTime.UtcNow`. `StartedAt` / `CompletedAt` של ביצוע ו-`CreatedAt` / `UpdatedAt` של תזכורת הם UTC.

## היסטוריית ביצוע

כל ניסיון ביצוע אמיתי יוצר `ReminderExecution` אחד (כולל ניסיונות Failed). `GET /api/reminders/{id}/executions` זמין ל-Admin ול-Viewer, מחזיר 404 אם התזכורת לא קיימת, ומסודר לפי `StartedAt` יורד (החדש ביותר ראשון). ה-API מחזיר DTOs (`Id`, `ReminderId`, `StartedAt`, `CompletedAt`, `Status`), לא ישויות EF.

## התנהגות כשל

תוצאת **Failed** מדומה עדיין נספרת כביצוע: היא נרשמת, `FutureRunsCount` יורד, ותזכורת מחזורית מתוזמנת מחדש אם נשארו ריצות. **אין ניסיון חוזר אוטומטי** של אותו ניסיון.

## הנחות

- **`FutureRunsCount` מסופק על ידי המשתמש.** הוא **אינו** מחושב מ-`Frequency` או מ-`ScheduledAt`. דוגמה: `Frequency = Daily` ו-`FutureRunsCount = 5` פירושם חמישה ביצועים עתידיים מתוכננים.
- ביצועים שנכשלו צורכים ריצה כמו ביצועים שהצליחו; אין ניסיון חוזר של אותו ניסיון.
- כש-`FutureRunsCount` מגיע ל-0 (Once אחרי הריצה היחידה, או מחזורי אחרי הריצה האחרונה), **`IsActive` נקבע ל-`false`**.
- תזכורות Once לא מתוזמנות מחדש; `FutureRunsCount` הנותר נכפה ל-0 אחרי הביצוע.
- ערכי **Frequency** נתמכים: `Once`, `Daily`, `Weekly`, `Monthly`.
- שליחת תזכורת היא **סימולציה בלבד** (`Task.Delay` של 10 שניות, ואז Success/Failed אקראי). אין אימייל, SMS או ספק התראות חיצוני.
- תזכורות חדשות תמיד מתחילות עם **`Status = Pending`**. הלקוח אינו יכול לקבוע סטטוס ביצירה או בעדכון.
- `ScheduledAt` חייב להיות `DateTime` תקין; **אין** חובה שיהיה בעתיד. זמן בעבר שימושי לבדיקה מקומית של המעבד.
- אורך מקסימלי לשם: 200 תווים; להודעה: 2000 תווים; `FutureRunsCount` חייב להיות מספר שלם לא-שלילי.
- פרטי התחברות שגויים מחזירים **401 Unauthorized** בלי להבחין בין משתמש לא קיים לסיסמה שגויה.
- נתוני In-Memory נמחקים בהפעלה מחדש של התהליך.
- הגבלות ממשק Angular אינן תחליף להרשאה ב-API.
- כיבוי במהלך ההמתנה המדומה אינו מסמן מיד את הביצוע כ-Failed. ביצוע `Running` בלי `CompletedAt` ישן מ-`StaleRunningThresholdSeconds` משוחזר כ-Failed והתזכורת חוזרת ל-Pending בלי להקטין את `FutureRunsCount`.

## גילוי שימוש ב-AI

במהלך המימוש נעשה שימוש בכלי פיתוח מבוססי AI, כולל **Cursor**. הם סייעו בתכנון, פיגום, יצירת קוד, רפקטורינג ודיבוג. הפתרון שהוגש נסקר והובן על ידי המפתחת; סיוע ה-AI לא החליף בעלות על העיצוב או על הקוד.

## נקודות קצה של ה-API

| Method | Path | תפקידים |
|--------|------|--------|
| POST | `/api/auth/login` | אנונימי |
| GET | `/api/reminders` | Admin, Viewer |
| GET | `/api/reminders/{id}` | Admin, Viewer |
| GET | `/api/reminders/{id}/executions` | Admin, Viewer |
| POST | `/api/reminders` | Admin |
| PUT | `/api/reminders/{id}` | Admin |
