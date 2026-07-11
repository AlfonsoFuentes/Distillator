using Distillator.Domain.Configuration;

namespace Distillator.Domain.Models
{
    public class User : IUser
    {
        private readonly List<IProject> _projects = new();

        public Guid Id { get; }
        public string Email { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string DisplayName => $"{FirstName} {LastName}".Trim();
        public bool IsAdministrator { get; }
        public bool IsActive { get; }
        public DateTime CreatedAt { get; }
        public IProjectConfiguration DefaultPreferences { get; }
        public IReadOnlyCollection<IProject> Projects => _projects.AsReadOnly();

        public User(Guid id, string email, string firstName, string lastName, bool isAdministrator, IProjectConfiguration? defaultPreferences = null)
        {
            Id = id;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            IsAdministrator = isAdministrator;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            DefaultPreferences = defaultPreferences ?? new ProjectConfiguration();
        }

    public IProject CreateProject(string name, IProjectConfiguration? configuration = null)
    {
        var project = new Project(name, this, configuration ?? DefaultPreferences);
        _projects.Add(project);
        return project;
    }

        public void RemoveProject(Guid projectId)
        {
            var project = _projects.FirstOrDefault(p => p.Id == projectId);
            if (project != null)
                _projects.Remove(project);
        }

        public IProject? GetProject(Guid id) => _projects.FirstOrDefault(p => p.Id == id);
        public IProject? GetProjectByName(string name) => _projects.FirstOrDefault(p => p.Name == name);
    }
}
