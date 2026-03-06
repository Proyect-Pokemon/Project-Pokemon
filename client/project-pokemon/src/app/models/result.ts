export class Result<T> {
    success: boolean;
    status: number;
    message: string;
    data: T | null;

    constructor(success: boolean, status: number, message: string, data: T | null) {
        this.success = success;
        this.status = status;
        this.message = message;
        this.data = data;
    }

    static success<T>(status: number, data: T): Result<T> {
        return new Result(true, status, 'Success', data);
    }

    static error(status: number, message: string): Result<any> {
        return new Result(false, status, message, null);
    }
}
