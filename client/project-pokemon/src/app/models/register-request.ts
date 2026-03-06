// Define los datos que se envían al backend para registrarse
export interface RegisterRequest {
    nickname: string;
    email: string;
    password: string;
}