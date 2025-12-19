using Microsoft.AspNetCore.Mvc;
using SignUP1_test.DTO;
using SignUP1_test.Models;
using SignUP1test.Data;
using SignUP1test.DTO;
using SignUP1test.Models;
using Microsoft.EntityFrameworkCore;


namespace SignUP1_test.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CoursesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.Categories)
                .Select(c => new GetCourseDto
                {
                    Id = c.CourseID,
                    CourseTitle = c.CourseTitle,
                    CourseSubtitle = c.CourseSubtitle,
                    ImgPath = c.ImgPath,
                    Level = c.Level,
                    Rating = (double)c.AvgRating,
                    Students = (int)c.Students,
                    Price = (double)c.Price,
                    Duration = c.Duration,
                    Instructor = c.Instructor,
                    Description=c.Description,
                    
                    Category = c.Categories.Select(cat => cat.Category).ToList()
                }).ToListAsync();

            return Ok(courses);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] GetCourseDto courseDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var course = new Course
            {
                CourseTitle = courseDto.CourseTitle,
                CourseSubtitle = courseDto.CourseSubtitle,
                ImgPath = courseDto.ImgPath,
                Level = courseDto.Level,
                AvgRating = courseDto.Rating,
                Students = courseDto.Students,
                Price = courseDto.Price,
                Duration = courseDto.Duration,
                Instructor = courseDto.Instructor,

                Categories = courseDto.Category.Select(c => new CourseCategory { Category = c }).ToList()
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCourses), new { id = course.CourseID }, courseDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] GetCourseDto courseDto)
        {
            var course = await _context.Courses.Include(c => c.Categories).FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
                return NotFound("Course not found.");

            course.CourseTitle = courseDto.CourseTitle;
            course.CourseSubtitle = courseDto.CourseSubtitle;
            course.ImgPath = courseDto.ImgPath;
            course.Level = courseDto.Level;
            course.AvgRating = courseDto.Rating;
            course.Students = courseDto.Students;
            course.Price = courseDto.Price;
            course.Duration = courseDto.Duration;
            course.Instructor = courseDto.Instructor;
            course.Description = courseDto.Description;

            // Update Categories
            _context.CourseCategories.RemoveRange(course.Categories);
            course.Categories = courseDto.Category.Select(c => new CourseCategory { Category = c, CourseID = id }).ToList();

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.Include(c => c.Categories).FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
                return NotFound("Course not found.");

            _context.CourseCategories.RemoveRange(course.Categories);
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourseDetails(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Categories)
                .Include(c => c.SyllabusWeeks)
                    .ThenInclude(sw => sw.Lessons)
                .Include(c => c.Reviews)
                    .ThenInclude(r => r.User) // <-- Loads user info for each review
                .Include(c => c.LearningOutcomes)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
                return NotFound("Course not found.");

            // Fetch instructor info (modify as needed for your structure)
            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(i => i.Name == course.Instructor);

            var response = new GetCourseDetailsDto
            {
                Id = course.CourseID,
                CourseTitle = course.CourseTitle,
                CourseSubtitle = course.CourseSubtitle,
                Price = course.Price,
                Duration = course.Duration,
                Level = course.Level,
                ImgPath = course.ImgPath,
                Rating = course.AvgRating,
                Students = course.Students,
                Description = course.Description,
                Category = course.Categories.Select(c => c.Category).ToList(),
                LearningOutcomes = course.LearningOutcomes.Select(lo => lo.Text).ToList(),

                InstructorId = instructor?.InstructorID,
                Instructor = instructor?.Name,
                InstructorTitle = instructor?.Title,
                InstructorBio = instructor?.Bio,
                InstructorImage = instructor?.ImagePath,
                InstructorRating = instructor?.Rating,
                InstructorStudents = instructor?.Students,
                InstructorCourses = instructor?.CoursesCount,

                Syllabus = course.SyllabusWeeks.Select(sw => new SyllabusWeekDto
                {
                    Week = sw.Week,
                    Title = sw.Title,
                    Lessons = sw.Lessons.Select(l => new LessonDto
                    {
                        Id = l.Id,
                        Title = l.Title,
                        Type = l.Type,
                        Duration = l.Duration,
                        VideoUrl = l.VideoUrl,
                        Thumbnail = l.Thumbnail,
                        Description = l.Description
                    }).ToList()
                }).ToList(),

                Reviews = course.Reviews.Select(r => new CourseReviewDto
                {
                    ReviewID = r.ReviewID,
                    UserID = r.UserID,
                    UserName = r.User?.FullName,
                    UserImage = r.User?.Image,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ReviewDate = r.ReviewDate
                }).ToList()
            };

            return Ok(response);
        }



        [HttpPost("review")]
        public async Task<IActionResult> PostReview([FromBody] CreateCourseReviewDto dto)
        {
            // (Optional) Validate user and course existence
            var userExists = await _context.Users.AnyAsync(u => u.UserID == dto.UserID);
            var courseExists = await _context.Courses.AnyAsync(c => c.CourseID == dto.CourseID);

            if (!userExists || !courseExists)
                return BadRequest("User or Course does not exist.");

            // You may want to check if the user already posted a review for this course
            var existingReview = await _context.CourseReviews
                .FirstOrDefaultAsync(r => r.UserID == dto.UserID && r.CourseID == dto.CourseID);

            if (existingReview != null)
                return BadRequest("User has already reviewed this course.");

            // Create and save the review
            var review = new CourseReview
            {
                UserID = dto.UserID,
                CourseID = dto.CourseID,
                Rating = dto.Rating,
                Comment = dto.Comment,
                ReviewDate = DateTime.UtcNow
            };

            _context.CourseReviews.Add(review);
            await _context.SaveChangesAsync();

            // Return success (you can return the created review or just Ok)
            return Ok(new { message = "Review submitted successfully." });
        }


    }
}
