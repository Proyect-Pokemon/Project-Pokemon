import { UserRole } from './get-admin-user-dto';

export type AdminUserRow = {
  id: number;
  nickname: string;
  email: string;
  avatarUrl: string;
  role: UserRole;
  creationDate: string;
};