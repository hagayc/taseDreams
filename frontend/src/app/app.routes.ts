import { Routes } from '@angular/router';
import { ProjectCreationComponent } from './project-creation/project-creation.component';
import { ProjectsListComponent } from './projects-list/projects-list.component';

export const routes: Routes = [
  { path: '', redirectTo: '/create', pathMatch: 'full' },
  { path: 'create', component: ProjectCreationComponent },
  { path: 'projects', component: ProjectsListComponent }
];

