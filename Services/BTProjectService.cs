using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheBugTracker.Data;
using TheBugTracker.Models;
using TheBugTracker.Models.Enums;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services
{
    public class BTProjectService : IBTProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBTRolesService _rolesService;

        public BTProjectService(ApplicationDbContext context, IBTRolesService rolesService)
        {
            _context = context;
            _rolesService = rolesService;
        }

        // CRUD = Create
        public async Task AddNewProjectAsync(Project project)
        {
            _context.Add(project);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AddProjectManagerAsync(string userId, int projectId)
        {
            BTUser currentPM = await GetProjectManagerAsync(projectId);

            // Remove current PM, if necessary
            if (currentPM != null)
            {
                try
                {
                    await RemoveProjectManagerAsync(projectId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"**** ERROR **** - Error removing User from project. ---> {ex.Message}");
                    return false;
                }
            }

            // Add the new PM
            try
            {
                await AddProjectManagerAsync(userId, projectId);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"**** ERROR **** - Error adding new Project Manager. ---> {ex.Message}");
                return false;
            }

        }

        public async Task<bool> AddUserToProjectAsync(string userId, int projectId)
        {
            BTUser user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null)
            {
                Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                if (!await IsUserOnProjectAsync(userId,projectId))
                {
                    try
                    {
                        project.Members.Add(user);
                        await _context.SaveChangesAsync();
                        return true;
                    } 
                    catch (Exception)
                    {
                        throw;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        // CRUD - Archive (Delete)
        public async Task ArchiveProjectAsync(Project project)
        {
            try
            {
                project.Archived = true;
                await UpdateProjectAsync(project);

                // Archive the Tickets for the project
                foreach (Ticket ticket in project.Tickets)
                {
                    ticket.ArchivedByProject = true;
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<BTUser>> GetAllProjectMembersExceptPMAsync(int projectId)
        {
            List<BTUser> developers = await GetProjectMembersByRoleAsync(projectId, Roles.Developer.ToString());
            List<BTUser> submitters = await GetProjectMembersByRoleAsync(projectId, Roles.Submitter.ToString());
            List<BTUser> admins = await GetProjectMembersByRoleAsync(projectId, Roles.Admin.ToString());

            List<BTUser> teamMembers = developers.Concat(submitters).Concat(admins).ToList();

            return teamMembers;
        }

        public async Task<List<Project>> GetAllProjectsByCompany(int companyId)
        {
            try
            {

            List<Project> projects = new();

            projects = await _context.Projects.Where(p => p.CompanyId == companyId && p.Archived == false)
                                   .Include(p => p.Members)
                                   .Include(p => p.Tickets)
                                        .ThenInclude(t => t.Comments)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.Attachments)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.History)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.Notifications)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.DeveloperUser)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.OwnerUser)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(p => p.TicketStatus)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.TicketPriority)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.TicketType)
                                   .Include(p => p.ProjectPriority)
                                   .ToListAsync();

            return projects;
            }
            catch (Exception)
            {

                throw;
            }

        }

        public async Task<List<Project>> GetAllProjectsByPriority(int companyId, string priorityName)
        {
            List<Project> projects = await GetAllProjectsByCompany(companyId);
            int priorityId = await LookupProjectPriorityId(priorityName);

            return projects.Where(p => p.ProjectPriorityId == priorityId).ToList();
        }

        public async Task<List<Project>> GetArchivedProjectsByCompanyAsync(int companyId)
        {
            try
            {
                List<Project> projects = projects = await _context.Projects.Where(p => p.CompanyId == companyId && p.Archived == true)
                                   .Include(p => p.Members)
                                   .Include(p => p.Tickets)
                                        .ThenInclude(t => t.Comments)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.Attachments)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.History)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.Notifications)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.DeveloperUser)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.OwnerUser)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(p => p.TicketStatus)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.TicketPriority)
                                    .Include(p => p.Tickets)
                                        .ThenInclude(t => t.TicketType)
                                   .Include(p => p.ProjectPriority)
                                   .ToListAsync();

                return projects;
            }
                catch (Exception)
            {

                throw;
            }

        }

        public Task<List<BTUser>> GetDevelopersOnProjectAsync(int projectId)
        {
            throw new NotImplementedException();
        }

        // CRUD - Read
        public async Task<Project> GetProjectByIdAsync(int projectId, int companyId)
        {
            try
            {
                Project project = await _context.Projects
                                           .Include(p => p.Tickets)
                                                .ThenInclude(t => t.TicketPriority)
                                            .Include(p => p.Tickets)
                                                .ThenInclude(t => t.TicketStatus)
                                            .Include(p => p.Tickets)
                                                .ThenInclude(t => t.TicketType)
                                            .Include(p => p.Tickets)
                                                .ThenInclude(t => t.DeveloperUser)
                                            .Include(p => p.Tickets)
                                                .ThenInclude(t => t.OwnerUser)
                                           .Include(p => p.Members)
                                           .Include(p => p.ProjectPriority)
                                           .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);

                return project;
            }
            catch (Exception)
            {

                throw;
            }   
        }

        public async Task<BTUser> GetProjectManagerAsync(int projectId)
        {
            Project project = await _context.Projects
                                            .Include(p => p.Members)
                                            .FirstOrDefaultAsync(p => p.Id == projectId);
            
            foreach (BTUser member in project?.Members)
            {
                if (await _rolesService.IsUserInRoleAsync(member, Roles.ProjectManager.ToString()))
                {
                    return member;
                }
            }

            return null;
        }

        public async Task<List<BTUser>> GetProjectMembersByRoleAsync(int projectId, string role)
        {
            Project project = await _context.Projects
                                            .Include(p => p.Members)
                                            .FirstOrDefaultAsync(p => p.Id == projectId);

            List<BTUser> members = new();

            foreach (var user in project.Members)
            {
                if (await _rolesService.IsUserInRoleAsync(user, role))
                {
                    members.Add(user);
                }
            }

            return members;
        }

        public Task<List<BTUser>> GetSubmittersOnProjectAsync(int projectId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Project>> GetUnassignedProjectsAsync(int companyId)
        {
            List<Project> result = new();
            List<Project> projects = new();

            try
            {
                projects = await _context.Projects.Include(p => p.ProjectPriority)
                                                  .Where(p => p.CompanyId == companyId)
                                                  .ToListAsync();

                foreach (Project project in projects)
                {
                    if ((await GetProjectMembersByRoleAsync(project.Id, nameof(Roles.ProjectManager))).Count == 0)
                    {
                        result.Add(project);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

            return result;
        }

        public async Task<List<Project>> GetUserProjectsAsync(string userId)
        {
            try
            {
                List<Project> userProjects = (await _context.Users
                                            .Include(u => u.Projects)
                                                .ThenInclude(p => p.Company)
                                            .Include(u => u.Projects)
                                                .ThenInclude(p => p.Members)
                                            .Include(u => u.Projects)
                                                .ThenInclude(p => p.Tickets)
                                            .Include(u => u.Projects)
                                                .ThenInclude(t => t.Tickets)
                                                    .ThenInclude(t => t.DeveloperUser)
                                            .Include(u => u.Projects)
                                                .ThenInclude(t => t.Tickets)
                                                    .ThenInclude(t => t.OwnerUser)
                                            .Include(u => u.Projects)
                                                .ThenInclude(t => t.Tickets)
                                                    .ThenInclude(t => t.TicketPriority)
                                            .Include(u => u.Projects)
                                                .ThenInclude(t => t.Tickets)
                                                    .ThenInclude(t => t.TicketStatus)
                                            .Include(u => u.Projects)
                                                .ThenInclude(t => t.Tickets)
                                                    .ThenInclude(t => t.TicketType)
                                            .FirstOrDefaultAsync(u => u.Id == userId)).Projects.ToList();
                return userProjects;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"**** ERROR **** - Error removing User from project. ---> {ex.Message}");
                throw;
            }
        }

        public async Task<List<BTUser>> GetUsersNotOnProjectAsync(int projectId, int companyId)
        {
            List<BTUser> users = await _context.Users.Where(u => u.Projects.All(p => p.Id != projectId)).ToListAsync();

            return users.Where(u => u.CompanyId == companyId).ToList();
        }

        public async Task<bool> IsAssignedProjectManagerAsync(string userId, int projectId)
        {
            try
            {
                string projectManagerId = (await GetProjectManagerAsync(projectId))?.Id;

                if (projectManagerId == userId)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> IsUserOnProjectAsync(string userId, int projectId)
        {
            Project project = await _context.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId);

            bool result = false;

            if (project != null)
            {
                result = project.Members.Any(m => m.Id == userId);
            }
            return result;
        }

        public async Task<int> LookupProjectPriorityId(string priorityName)
        {
            int priorityId = (await _context.ProjectPriorities.FirstOrDefaultAsync(p => p.Name == priorityName)).Id;
            return priorityId;
        }

        public async Task RemoveProjectManagerAsync(int projectId)
        {
            Project project = await _context.Projects
                                            .Include(p => p.Members)
                                            .FirstOrDefaultAsync(p => p.Id == projectId);

            try
            {
                foreach (BTUser member in project?.Members)
                {
                    if (await _rolesService.IsUserInRoleAsync(member, Roles.ProjectManager.ToString()))
                    {
                        await RemoveUserFromProjectAsync(member.Id, projectId);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task RemoveUserFromProjectAsync(string userId, int projectId)
        {
            try
            {
                BTUser user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

                try
                {
                    if (await IsUserOnProjectAsync(userId, projectId))
                    {
                        project.Members.Remove(user);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception)
                {
                    throw;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"**** ERROR **** - Error removing User from project. ---> {ex.Message}");
            }
        }

        public async Task RemoveUsersFromProjectByRoleAsync(string role, int projectId)
        {
            try
            {
                List<BTUser> members = await GetProjectMembersByRoleAsync(projectId, role);
                Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

                foreach (BTUser btuser in members)
                {
                    try
                    {
                        project.Members.Remove(btuser);
                        await _context.SaveChangesAsync();                            
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"**** ERROR **** - Error removing User from project. ---> {ex.Message}");
                throw;
            }
        }

        public async Task RestoreProjectAsync(Project project)
        {
            try
            {
                project.Archived = false;
                await UpdateProjectAsync(project);

                // Archive the Tickets for the project
                foreach (Ticket ticket in project.Tickets)
                {
                    ticket.ArchivedByProject = false;
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        // CRUD - Update
        public async Task UpdateProjectAsync(Project project)
        {
            _context.Update(project);
            await _context.SaveChangesAsync();
        }
    }
}
