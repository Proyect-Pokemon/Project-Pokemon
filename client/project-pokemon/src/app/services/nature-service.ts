import { Injectable } from '@angular/core';
import { BaseApiService } from './base-api.service';
import { Nature } from '../models/nature';

@Injectable({
    providedIn: 'root',
})
export class NatureService extends BaseApiService {
    async getAllNatures(): Promise<Nature[]> {
        return this.getList<Nature>('nature');
    }
}