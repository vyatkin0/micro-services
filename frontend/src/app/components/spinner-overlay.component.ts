import { Component, Input } from '@angular/core';

//https://christianlydemann.com/four-ways-to-create-loading-spinners-in-an-angular-app/
//https://gist.github.com/lydemann/587eaa2bc686094226e6bd02fe53ef9a#file-spinner-overlay-service-ts

@Component({
  selector: 'app-spinner',
  templateUrl: './spinner-overlay.component.html',
  styleUrls: []
})
export class SpinnerOverlayComponent {
  @Input() message = '';
}