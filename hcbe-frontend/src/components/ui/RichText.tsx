import { useRef, useState } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

const allowedElements = ['p', 'h2', 'h3', 'h4', 'ul', 'ol', 'li', 'strong', 'em', 'blockquote', 'a', 'hr', 'br', 'code'];

export const plainTextFromRichText = (value: string) => value
  .replace(/!\[[^\]]*\]\([^)]*\)/g, '')
  .replace(/\[([^\]]+)\]\([^)]*\)/g, '$1')
  .replace(/^\s{0,3}(#{1,6}|>|[-+*]|\d+[.)])\s+/gm, '')
  .replace(/[*_~`]/g, '')
  .replace(/\s+/g, ' ')
  .trim();

export function RichTextContent({ value, className = '' }: { value?: string | null; className?: string }) {
  if (!value) return null;
  return (
    <div className={`rich-text-content ${className}`}>
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        skipHtml
        allowedElements={allowedElements}
        unwrapDisallowed
        components={{ a: ({ children, ...props }) => <a {...props} target="_blank" rel="noreferrer">{children}</a> }}
      >
        {value}
      </ReactMarkdown>
    </div>
  );
}

type Format = 'bold' | 'italic' | 'heading' | 'bullet' | 'ordered' | 'quote' | 'link';

interface RichTextEditorProps {
  id: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  required?: boolean;
  maxLength?: number;
  minHeight?: number;
  className?: string;
  label?: string;
}

const formats: Array<{ name: Format; icon: string; fr: string; en: string }> = [
  { name: 'bold', icon: 'ri-bold', fr: 'Gras', en: 'Bold' },
  { name: 'italic', icon: 'ri-italic', fr: 'Italique', en: 'Italic' },
  { name: 'heading', icon: 'ri-heading', fr: 'Intertitre', en: 'Heading' },
  { name: 'bullet', icon: 'ri-list-unordered', fr: 'Liste à puces', en: 'Bulleted list' },
  { name: 'ordered', icon: 'ri-list-ordered-2', fr: 'Liste numérotée', en: 'Numbered list' },
  { name: 'quote', icon: 'ri-double-quotes-l', fr: 'Citation', en: 'Quote' },
  { name: 'link', icon: 'ri-link', fr: 'Lien', en: 'Link' },
];

export function RichTextEditor({ id, value, onChange, placeholder, required, maxLength, minHeight = 240, className = '', label }: RichTextEditorProps) {
  const textarea = useRef<HTMLTextAreaElement>(null);
  const [preview, setPreview] = useState(false);
  const french = document.documentElement.lang !== 'en';

  const format = (kind: Format) => {
    const element = textarea.current;
    if (!element) return;
    const start = element.selectionStart;
    const end = element.selectionEnd;
    const selection = value.slice(start, end);
    let before = '';
    let after = '';
    let replacement = selection;

    if (kind === 'bold') { before = '**'; after = '**'; replacement ||= french ? 'texte important' : 'important text'; }
    if (kind === 'italic') { before = '_'; after = '_'; replacement ||= french ? 'texte' : 'text'; }
    if (kind === 'link') { before = '['; after = '](https://)'; replacement ||= french ? 'texte du lien' : 'link text'; }
    if (kind === 'heading') { before = '## '; replacement ||= french ? 'Intertitre' : 'Heading'; }
    if (kind === 'quote') { before = '> '; replacement ||= french ? 'Citation' : 'Quote'; }
    if (kind === 'bullet') { before = '- '; replacement = (selection || (french ? 'Élément de liste' : 'List item')).replace(/\n/g, '\n- '); }
    if (kind === 'ordered') { before = '1. '; replacement = (selection || (french ? 'Élément de liste' : 'List item')).split('\n').map((line, index) => `${index + 1}. ${line}`).join('\n'); }

    const next = `${value.slice(0, start)}${before}${replacement}${after}${value.slice(end)}`;
    if (maxLength && next.length > maxLength) return;
    onChange(next);
    requestAnimationFrame(() => {
      element.focus();
      element.setSelectionRange(start + before.length, start + before.length + replacement.length);
    });
  };

  return (
    <div className={`rich-text-editor overflow-hidden rounded-[16px] border border-outline bg-surface shadow-[0_8px_24px_rgba(0,59,27,.045)] transition focus-within:border-green focus-within:ring-2 focus-within:ring-green/15 ${className}`}>
      <div className="flex min-h-12 flex-wrap items-center justify-between gap-2 border-b border-line/70 bg-surface-container/70 px-2.5 py-2">
        <div className="flex flex-wrap items-center gap-1" role="toolbar" aria-label={french ? 'Mise en forme du texte' : 'Text formatting'}>
          {formats.map((item) => (
            <button key={item.name} type="button" title={french ? item.fr : item.en} aria-label={french ? item.fr : item.en} onClick={() => format(item.name)} className="inline-flex h-8 w-8 items-center justify-center rounded-lg text-sm text-ink-variant transition hover:bg-surface hover:text-green focus-visible:outline-offset-1">
              <i className={item.icon} aria-hidden="true" />
            </button>
          ))}
        </div>
        <div className="flex rounded-lg border border-line bg-surface p-0.5 text-[9px] font-bold uppercase tracking-[.1em]">
          <button type="button" onClick={() => setPreview(false)} className={`rounded-md px-2.5 py-1.5 transition ${!preview ? 'bg-green text-white' : 'text-ink-variant'}`}>{french ? 'Écrire' : 'Write'}</button>
          <button type="button" onClick={() => setPreview(true)} className={`rounded-md px-2.5 py-1.5 transition ${preview ? 'bg-green text-white' : 'text-ink-variant'}`}>{french ? 'Aperçu' : 'Preview'}</button>
        </div>
      </div>
      {preview ? (
        <div className="overflow-y-auto px-5 py-4" style={{ minHeight }} aria-label={french ? `Aperçu ${label || ''}` : `${label || ''} preview`}>
          {value ? <RichTextContent value={value} /> : <p className="text-sm italic text-ink-variant/70">{french ? 'Commencez à écrire pour afficher l’aperçu.' : 'Start writing to see a preview.'}</p>}
        </div>
      ) : (
        <textarea data-rich-text-input ref={textarea} id={id} value={value} onChange={(event) => onChange(event.target.value)} placeholder={placeholder} required={required} maxLength={maxLength} className="block w-full resize-y bg-transparent px-5 py-4 text-[15px] leading-7 text-ink outline-none placeholder:text-ink-variant/55" style={{ minHeight }} aria-label={label} />
      )}
      <div className="flex min-h-9 items-center justify-between gap-3 border-t border-line/60 bg-surface-container/45 px-4 py-2 text-[10px] text-ink-variant">
        <span className="inline-flex items-center gap-1.5"><i className="ri-markdown-line text-green" aria-hidden="true" />{french ? 'Mise en forme sécurisée' : 'Safe formatting'}</span>
        {maxLength && <span className="tabular-nums">{value.length}/{maxLength}</span>}
      </div>
    </div>
  );
}
