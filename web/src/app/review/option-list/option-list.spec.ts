import { ComponentFixture, TestBed } from '@angular/core/testing';
import { OptionList } from './option-list';

describe('OptionList', () => {
  let fixture: ComponentFixture<OptionList>;
  const options = ['Stack', 'Heap', 'Large object heap', '`Span<T>`'];

  const buttons = () =>
    Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>(
        'button[role=radio]',
      ),
    );

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [OptionList] }).compileComponents();
    fixture = TestBed.createComponent(OptionList);
    fixture.componentRef.setInput('options', options);
    await fixture.whenStable();
  });

  it('renders one radio button per option with A–D key hints and inline code', () => {
    expect(buttons()).toHaveLength(4);
    expect(buttons().map((b) => b.textContent?.trim().charAt(0))).toEqual(['A', 'B', 'C', 'D']);
    expect(buttons()[3].querySelector('code')?.textContent).toBe('Span<T>');
    expect(fixture.nativeElement.querySelector('[role=radiogroup]')).not.toBeNull();
  });

  it('emits the index on click', () => {
    const picked: number[] = [];
    fixture.componentInstance.selectedChange.subscribe((i) => picked.push(i));

    buttons()[2].click();

    expect(picked).toEqual([2]);
  });

  it('marks the selected option and its aria-checked state', async () => {
    fixture.componentRef.setInput('selected', 1);
    await fixture.whenStable();

    expect(buttons()[1].getAttribute('aria-checked')).toBe('true');
    expect(buttons()[1].dataset['state']).toBe('selected');
    expect(buttons()[1].className).toContain('border-accent');
    expect(buttons()[0].getAttribute('aria-checked')).toBe('false');
    expect(buttons()[0].dataset['state']).toBe('neutral');
  });

  it('ignores clicks when locked', async () => {
    fixture.componentRef.setInput('locked', true);
    await fixture.whenStable();
    const picked: number[] = [];
    fixture.componentInstance.selectedChange.subscribe((i) => picked.push(i));

    buttons()[0].click();

    expect(buttons().every((b) => b.disabled)).toBe(true);
    expect(picked).toEqual([]);
  });

  it('shows the correct option in the ok color and a wrong selection in the bad color', async () => {
    fixture.componentRef.setInput('selected', 3);
    fixture.componentRef.setInput('correctIndex', 0);
    fixture.componentRef.setInput('locked', true);
    await fixture.whenStable();

    expect(buttons()[0].dataset['state']).toBe('correct');
    expect(buttons()[0].className).toContain('bg-ok-soft');
    expect(buttons()[0].textContent).toContain('(correct answer)');
    expect(buttons()[3].dataset['state']).toBe('wrong');
    expect(buttons()[3].className).toContain('bg-bad-soft');
    expect(buttons()[1].dataset['state']).toBe('neutral');
  });

  it('shows only the ok color when the selection was right', async () => {
    fixture.componentRef.setInput('selected', 0);
    fixture.componentRef.setInput('correctIndex', 0);
    await fixture.whenStable();

    expect(buttons()[0].dataset['state']).toBe('correct');
    expect(buttons().filter((b) => b.dataset['state'] === 'wrong')).toHaveLength(0);
  });
});
