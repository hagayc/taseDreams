import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService, ProjectLane, InfrastructureService } from '../services/api.service';

@Component({
  selector: 'app-project-creation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './project-creation.component.html',
  styleUrl: './project-creation.component.css'
})
export class ProjectCreationComponent implements OnInit {
  projectName: string = '';
  selectedTeam: string = '';
  selectedLane: ProjectLane | null = null;
  selectedBackendVersion: string = '';
  selectedFrontendVersion: string = '';
  selectedInfrastructureServices: number[] = [];

  teams: string[] = [];
  lanes: ProjectLane[] = [];
  infrastructureServices: InfrastructureService[] = [];
  backendVersions: string[] = [];
  frontendVersions: string[] = [];

  maxProjectNameLength: number = 12;
  isSubmitting: boolean = false;
  errorMessage: string = '';
  errorDetails: string = '';
  successMessage: string = '';

  constructor(
    private apiService: ApiService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.apiService.getTeams().subscribe(teams => {
      this.teams = teams;
    });

    this.apiService.getProjectLanes().subscribe(lanes => {
      this.lanes = lanes;
    });

    this.apiService.getInfrastructureServices().subscribe(services => {
      this.infrastructureServices = services;
    });
  }

  onLaneChange() {
    if (this.selectedLane) {
      this.apiService.getVersions(this.selectedLane.backendLanguage).subscribe(versions => {
        this.backendVersions = versions;
      });

      this.apiService.getVersions(this.selectedLane.frontendFramework).subscribe(versions => {
        this.frontendVersions = versions;
      });

      // Reset selections
      this.selectedBackendVersion = '';
      this.selectedFrontendVersion = '';
    }
  }

  toggleInfrastructureService(serviceId: number) {
    const index = this.selectedInfrastructureServices.indexOf(serviceId);
    if (index > -1) {
      this.selectedInfrastructureServices.splice(index, 1);
    } else {
      this.selectedInfrastructureServices.push(serviceId);
    }
  }

  isInfrastructureServiceSelected(serviceId: number): boolean {
    return this.selectedInfrastructureServices.includes(serviceId);
  }

  validateForm(): boolean {
    if (!this.projectName || this.projectName.length === 0) {
      this.errorMessage = 'Project name is required';
      return false;
    }

    if (this.projectName.length > this.maxProjectNameLength) {
      this.errorMessage = `Project name cannot exceed ${this.maxProjectNameLength} characters`;
      return false;
    }

    if (!/^[a-z0-9]+$/.test(this.projectName)) {
      this.errorMessage = 'Project name must contain only lowercase letters and numbers';
      return false;
    }

    if (!this.selectedTeam) {
      this.errorMessage = 'Please select a development team';
      return false;
    }

    if (!this.selectedLane) {
      this.errorMessage = 'Please select a project lane';
      return false;
    }

    if (!this.selectedBackendVersion) {
      this.errorMessage = 'Please select a backend version';
      return false;
    }

    if (!this.selectedFrontendVersion) {
      this.errorMessage = 'Please select a frontend version';
      return false;
    }

    return true;
  }

  onSubmit() {
    this.errorMessage = '';
    this.errorDetails = '';
    this.successMessage = '';

    if (!this.validateForm()) {
      return;
    }

    this.isSubmitting = true;

    const request = {
      projectName: this.projectName.toLowerCase(),
      developmentTeam: this.selectedTeam,
      projectLaneId: this.selectedLane!.id,
      backendVersion: this.selectedBackendVersion,
      frontendVersion: this.selectedFrontendVersion,
      infrastructureServiceIds: this.selectedInfrastructureServices
    };

    this.apiService.createProject(request).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.status === 'Completed') {
          this.successMessage = `Project "${response.projectName}" created successfully!`;
          setTimeout(() => {
            this.router.navigate(['/projects']);
          }, 2000);
        } else {
          this.errorMessage = response.message || 'Project creation failed';
          this.errorDetails = response.errorDetails || '';
        }
      },
      error: (error) => {
        this.isSubmitting = false;
        // Try to extract error from response body first
        if (error.error) {
          this.errorMessage = error.error.message || 'An error occurred while creating the project';
          this.errorDetails = error.error.errorDetails || error.error.title || JSON.stringify(error.error, null, 2);
        } else {
          this.errorMessage = error.message || 'An error occurred while creating the project';
          this.errorDetails = error.toString();
        }
      }
    });
  }
}

