import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';
import { GetAdminUserDto } from '../models/get-admin-user-dto';
import { PutUserRoleDto } from '../models/put-user-role-dto';

@Injectable({
  providedIn: 'root',
})
export class AdminService {
  private readonly api = inject(ApiService);

  buildAvatarUrl(avatarPath: string | null | undefined): string {
    const name = avatarPath?.trim();
    if (!name) return '/assets/Images/avatar-default.jpg';
    if (name.startsWith('/assets/')) return name;
    return `/assets/Images/${name}`;
  }

  async getAllUsers(): Promise<GetAdminUserDto[]> {
    return this.api.get<GetAdminUserDto[]>('admin');
  }

  async updateUserRole(userId: number, dto: PutUserRoleDto): Promise<PutUserRoleDto> {
    return this.api.put<PutUserRoleDto>(`admin/${userId}Role`, dto);
  }

  async deleteUser(userId: number): Promise<void> {
    await this.api.delete<void>(`admin/${userId}`);
  }
}