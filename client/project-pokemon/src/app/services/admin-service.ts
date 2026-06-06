import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';
import { GetAdminUserDto } from '../models/get-admin-user-dto';
import { PutUserRoleDto } from '../models/put-user-role-dto';

@Injectable({
  providedIn: 'root',
})
export class AdminService {
  private readonly api = inject(ApiService);

  private readonly API_ORIGIN = 'https://localhost:7277';

  buildAvatarUrl(avatarPath: string | null | undefined): string {
    const clean = avatarPath?.trim();
    if (!clean) {
      return '/assets/images/avatar-default.png';
    }
    if (clean.startsWith('/assets/')) {
      return clean;
    }
    const fileName = clean
      .replace(/^https?:\/\/localhost:7277\/uploads\//i, '')
      .replace(/^\/?uploads\//i, '');
    return `${this.API_ORIGIN}/uploads/${fileName}`;
  }

  async getAllUsers(): Promise<GetAdminUserDto[]> {
    return this.api.get<GetAdminUserDto[]>('admin');
  }

  async updateUserRole(userId: number, dto: PutUserRoleDto): Promise<PutUserRoleDto> {
    return this.api.put<PutUserRoleDto>(`admin/${userId}/role`, dto);
  }

  async deleteUser(userId: number): Promise<void> {
    await this.api.delete<void>(`admin/${userId}`);
  }
}