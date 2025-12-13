import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

const API_URL = 'http://localhost:5000/api';

export interface ProjectLane {
  id: number;
  name: string;
  backendLanguage: string;
  frontendFramework: string;
}

export interface InfrastructureService {
  id: number;
  name: string;
  type: string;
}

export interface ProjectCreationRequest {
  projectName: string;
  developmentTeam: string;
  projectLaneId: number;
  backendVersion: string;
  frontendVersion: string;
  infrastructureServiceIds: number[];
}

export interface ProjectCreationResponse {
  projectId: number;
  projectName: string;
  status: string;
  message: string;
  errorDetails?: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  username: string;
  groups: string[];
  authenticated: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  constructor(private http: HttpClient) {}

  getProjectLanes(): Observable<ProjectLane[]> {
    return this.http.get<ProjectLane[]>(`${API_URL}/projectlanes`);
  }

  getInfrastructureServices(): Observable<InfrastructureService[]> {
    return this.http.get<InfrastructureService[]>(`${API_URL}/infrastructureservices`);
  }

  getTeams(): Observable<string[]> {
    return this.http.get<string[]>(`${API_URL}/teams`);
  }

  getVersions(language: string): Observable<string[]> {
    // This would typically come from the backend, but for now we'll handle it client-side
    const versions: Record<string, string[]> = {
      'dotnet': ['10.0', '8.0', '6.0'],
      'java': ['21', '17', '11'],
      'angular': ['19', '18', '17'],
      'react': ['19', '18', '17']
    };
    return new Observable(observer => {
      observer.next(versions[language] || []);
      observer.complete();
    });
  }

  createProject(request: ProjectCreationRequest): Observable<ProjectCreationResponse> {
    return this.http.post<ProjectCreationResponse>(`${API_URL}/projects`, request);
  }

  getProjects(): Observable<any[]> {
    return this.http.get<any[]>(`${API_URL}/projects`);
  }

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${API_URL}/auth/login`, request);
  }
}
