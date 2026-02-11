// Define el formato de respuesta de la API
export class Result<T = void> {
    success: boolean = false;
    statusCode: number = 0;
    error: string | undefined = undefined;
    data: T | undefined = undefined;

    private constructor(success: boolean, statusCode: number, error: string | undefined = undefined, data: T | undefined = undefined) {
        this.success = success;
        this.error = error;
        this.statusCode = statusCode;
        this.data = data;
    }

    throwIfError() {
        if (!this.success) {
        throw new Error(this.error);
        }
    }

    static success<T = void>(statusCode: number, data?: T): Result<T> {
        return new Result(true, statusCode, undefined, data);
    } 

    static error<T = void>(statusCode: number, error?: string): Result<T> {
        return new Result(false, statusCode, error);
    } 
}

// PREGUNTAR A JOSE SI HAY OTRA FORMA:
// He tenido que cambiar el restricted a false en el tsconfig.json para poder usar
// la clase sin tener que inicializar sus propiedades
