using Microsoft.EntityFrameworkCore;
using PRN232.LMS.Repositories.Entities;

namespace PRN232.LMS.Repositories.Data;

/// <summary>
/// Seeds the database with the minimum data required by Lab 1:
/// 5 semesters, 10 subjects, 20 courses, 50 students and 500 enrollments.
/// This is pure data-access code (no business logic).
/// </summary>
public static class DbSeeder
{
    public static async Task EnsureSeededAsync(AppDbContext db)
    {
        // Create the database if it does not exist yet.
        await db.Database.EnsureCreatedAsync();

        if (await db.Semesters.AnyAsync() || await db.Students.AnyAsync())
        {
            return; // already seeded
        }

        var rng = new Random(2024);

        // ---------- 5 Semesters ----------
        var (semNames, semStarts) = (
            new[] { "Fall2023", "Spring2024", "Summer2024", "Fall2024", "Spring2025" },
            new[] { new DateTime(2023, 9, 4), new DateTime(2024, 1, 8), new DateTime(2024, 5, 6), new DateTime(2024, 9, 2), new DateTime(2025, 1, 6) });

        var semesters = semNames.Select((name, i) => new Semester
        {
            SemesterName = name,
            StartDate = semStarts[i],
            EndDate = semStarts[i].AddMonths(4).AddDays(-1)
        }).ToList();
        db.Semesters.AddRange(semesters);
        await db.SaveChangesAsync(); // get SemesterId values

        // ---------- 10 Subjects ----------
        var subjectData = new (string Code, string Name, int Credit)[]
        {
            ("PRN211", "Windows Programming with C#", 3),
            ("PRN231", "Web Application Development with .NET", 3),
            ("PRN232", "Web API Development with .NET", 3),
            ("PRN292", "C# Programming", 3),
            ("PRO192", "Object-Oriented Programming", 3),
            ("CSD201", "Data Structures and Algorithms", 3),
            ("DBI202", "Introduction to Databases", 3),
            ("SWD392", "Software Architecture and Design", 3),
            ("MAE101", "Mathematics for Engineering", 3),
            ("JPD113", "Elementary Japanese 1", 4)
        };

        var subjects = subjectData.Select(s => new Subject
        {
            SubjectCode = s.Code,
            SubjectName = s.Name,
            Credit = s.Credit
        }).ToList();
        db.Subjects.AddRange(subjects);
        await db.SaveChangesAsync();

        // ---------- 20 Courses (each course belongs to one semester) ----------
        var semestersById = semesters.ToDictionary(s => s.SemesterId);
        var courses = new List<Course>();
        for (int i = 0; i < 20; i++)
        {
            var subject = subjects[i % subjects.Count];
            var semester = semesters[rng.Next(semesters.Count)];
            courses.Add(new Course
            {
                // Course name re-uses the subject-like code so the course is easy to read.
                CourseName = $"{subject.SubjectCode} - {semester.SemesterName} - Group {(i / 10) + 1}",
                SemesterId = semester.SemesterId
            });
        }
        db.Courses.AddRange(courses);
        await db.SaveChangesAsync();

        // ---------- 50 Students (several "Nguyen ..." names so ?search=nguyen returns data) ----------
        string[] lastNames = { "Nguyen", "Nguyen", "Nguyen", "Nguyen", "Nguyen", "Nguyen", "Nguyen", "Nguyen", "Tran", "Tran", "Tran", "Tran", "Tran", "Tran", "Le", "Le", "Le", "Le", "Le", "Pham", "Pham", "Pham", "Pham", "Hoang", "Hoang", "Hoang", "Hoang", "Ngo", "Ngo", "Vu", "Vu", "Do", "Do", "Dang", "Dang", "Bui", "Ly", "Vo", "Mai", "Dinh", "Luu", "Truong", "Phung", "Duong", "Ha", "Nong", "Cao", "Quach", "Lam", "Van" };
        string[] givenNames = { "Van An", "Thi Binh", "Duc Minh", "Quoc Huy", "Thanh Tung", "Hong Nhung", "Xuan Mai", "Minh Chau", "Gia Bao", "Phuong Linh", "Khanh Vy", "Tuan Kiet", "Ngoc Anh", "Duy Khang", "Hoang Long", "Thu Ha", "Ba Loc", "Tien Dat", "My Duyen", "Anh Tu", "Cong Thanh", "Van Son", "Kim Oanh", "Tri Dung", "Hai Yen", "Quang Vinh", "Nhat Minh", "Thao Vy", "Phuc Nguyen", "Minh Thu", "Gia Huy", "Lan Anh", "Viet Hoang", "Thu Trang", "Dang Khoa", "My Linh", "Quoc Bao", "Huong Giang", "Thanh Hieu", "Ngoc Tram", "Cao Cuong", "Diem Quynh", "The Anh", "Thu Phuong", "Hoang Nam", "Bich Ngoc", "Van Tai", "Trong Nghia", "Minh Quan", "Tuyet Nhi" };

        var students = new List<Student>();
        string[] codePrefixes = { "SE", "CE", "IA", "AI", "MC", "GD" };
        for (int i = 0; i < 50; i++)
        {
            var dob = new DateTime(rng.Next(1999, 2006), rng.Next(1, 13), rng.Next(1, 29));
            students.Add(new Student
            {
                FullName = $"{lastNames[i]} {givenNames[i]}",
                Email = $"student{i + 1:000}@fpt.edu.vn",
                DateOfBirth = dob,
                // FPTU-style student code, e.g. SE19001, CE19002, IA19003 ...
                StudentCode = $"{codePrefixes[i % codePrefixes.Length]}{19001 + i}",
                PhoneNumber = $"09{rng.Next(10000000, 99999999)}"
            });
        }
        db.Students.AddRange(students);
        await db.SaveChangesAsync();

        // ---------- 500 Enrollments ----------
        var enrollments = new List<Enrollment>();
        string[] statuses = { "Active", "Active", "Active", "Active", "Active", "Active", "Completed", "Completed", "Dropped", "Pending" };

        for (int i = 0; i < 500; i++)
        {
            var student = students[rng.Next(students.Count)];
            var course = courses[rng.Next(courses.Count)];

            // EnrollDate inside the course semester window (approx.)
            var enrollDate = semestersById[course.SemesterId].StartDate.AddDays(rng.Next(0, 30));

            enrollments.Add(new Enrollment
            {
                StudentId = student.StudentId,
                CourseId = course.CourseId,
                EnrollDate = enrollDate,
                Status = statuses[rng.Next(statuses.Length)]
            });
        }
        db.Enrollments.AddRange(enrollments);
        await db.SaveChangesAsync();
    }
}
