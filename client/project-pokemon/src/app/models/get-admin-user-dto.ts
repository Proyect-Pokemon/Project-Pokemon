export type UserRole = 'admin' | 'user';

export interface GetAdminUserDto {
  id: number;
  nickname: string;
  email: string;
  role: string;
  avatarPath: string | null;
  creationDate: string;
}