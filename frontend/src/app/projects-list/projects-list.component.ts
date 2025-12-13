import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ApiService } from '../services/api.service';

@Component({
  selector: 'app-projects-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './projects-list.component.html',
  styleUrl: './projects-list.component.css'
})
export class ProjectsListComponent implements OnInit {
  projects: any[] = [];
  isLoading: boolean = true;

  constructor(
    private apiService: ApiService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadProjects();
  }

  loadProjects() {
    this.isLoading = true;
    this.apiService.getProjects().subscribe({
      next: (projects) => {
        this.projects = projects;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading projects:', error);
        this.isLoading = false;
      }
    });
  }

  getStatusClass(status: string): string {
    const statusMap: Record<string, string> = {
      'Pending': 'status-pending',
      'CreatingBitbucket': 'status-creating',
      'CreatingArtifactory': 'status-creating',
      'ConfiguringArgoCD': 'status-creating',
      'ConfiguringOCP': 'status-creating',
      'CreatingConfluence': 'status-creating',
      'Completed': 'status-completed',
      'Failed': 'status-failed'
    };
    return statusMap[status] || 'status-pending';
  }

  createNew() {
    this.router.navigate(['/create']);
  }
}

