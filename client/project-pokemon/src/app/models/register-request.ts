// Define los datos que se envían al backend para registrarse
export interface RegisterRequest {
    email: string;
    nickname: string;
    password: string;
    avatarPath?: string;
    biography?: string;
}