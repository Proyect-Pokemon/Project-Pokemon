export interface Move {
    name: string;
    description: string;
    power: number | null;
    accuracy: number | null;
    moveClass: string;
    pp: number;
    currentPp: number;
    type: string;
}
