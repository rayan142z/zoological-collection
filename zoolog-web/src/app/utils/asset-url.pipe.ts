import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../environments/environment';

/** Turns a stored photo path ("/images/specimens/x.png") into a loadable URL for the current environment. */
@Pipe({ name: 'assetUrl' })
export class AssetUrlPipe implements PipeTransform {
  transform(path: string | null | undefined): string {
    if (!path) return '';
    if (/^(https?:|data:|blob:)/.test(path)) return path;
    return environment.assetBase ? environment.assetBase + (path.startsWith('/') ? path : '/' + path) : path.replace(/^\//, '');
  }
}
