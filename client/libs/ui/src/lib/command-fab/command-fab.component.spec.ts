import { provideYumneyIcons } from '../icons/provide-icons';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommandFabComponent } from './command-fab.component';
import { ChatStateService, setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  commandBar: {
    toggle: 'Open command bar',
  },
};

describe('CommandFabComponent', () => {
  let fixture: ComponentFixture<CommandFabComponent>;
  let chatState: ChatStateService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommandFabComponent, setupTranslocoTesting(en)],
      providers: [provideYumneyIcons(), ChatStateService],
    }).compileComponents();

    fixture = TestBed.createComponent(CommandFabComponent);
    chatState = TestBed.inject(ChatStateService);
  });

  afterEach(() => {
    chatState.close();
  });

  it('should create the component', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the FAB button', () => {
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('.command-fab');
    expect(button).toBeTruthy();
  });

  it('should toggle chat state on click', () => {
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('.command-fab');

    button.click();
    expect(chatState.isOpen()).toBe(true);

    button.click();
    expect(chatState.isOpen()).toBe(false);
  });

  it('should add is-open class when chat is open', () => {
    chatState.open();
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('.command-fab');
    expect(button.classList.contains('is-open')).toBe(true);
  });

  it('should not have is-open class when chat is closed', () => {
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('.command-fab');
    expect(button.classList.contains('is-open')).toBe(false);
  });

  it('should have aria-expanded matching chat state', () => {
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('.command-fab');
    expect(button.getAttribute('aria-expanded')).toBe('false');

    chatState.open();
    fixture.detectChanges();
    expect(button.getAttribute('aria-expanded')).toBe('true');
  });

  it('should show message-circle icon when closed', () => {
    fixture.detectChanges();
    const icon = fixture.nativeElement.querySelector('lucide-icon');
    expect(icon.getAttribute('name')).toBe('message-circle');
  });

  it('should show x icon when open', () => {
    chatState.open();
    fixture.detectChanges();
    const icon = fixture.nativeElement.querySelector('lucide-icon');
    expect(icon.getAttribute('name')).toBe('x');
  });
});
