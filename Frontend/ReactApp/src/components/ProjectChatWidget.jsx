import { useEffect, useState } from 'react';
import apiClient from '../services/apiClient';

const suggestions = [
  'List active projects and deadlines',
  'Show overdue tasks',
  'What is the status of each sprint?'
];

function ProjectChatWidget() {
  const [open, setOpen] = useState(false);
  const [question, setQuestion] = useState('');
  const [messages, setMessages] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!open) return undefined;
    function closeOnEscape(event) {
      if (event.key === 'Escape') setOpen(false);
    }
    window.addEventListener('keydown', closeOnEscape);
    return () => window.removeEventListener('keydown', closeOnEscape);
  }, [open]);

  async function ask(value = question) {
    const text = value.trim();
    if (!text || loading) return;
    setQuestion('');
    setError('');
    setMessages((current) => [...current, { role: 'you', text }]);
    setLoading(true);
    try {
      const { data } = await apiClient.post('/project-chat', { question: text });
      setMessages((current) => [...current, { role: 'assistant', text: data.answer }]);
    } catch (requestError) {
      setError(requestError.response?.data?.message || 'Chat could not respond. Please try again.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="project-chat-widget">
      {open && (
        <section className="project-chat-window" role="dialog" aria-label="Project assistant">
          <header className="project-chat-widget-header">
            <div><strong>Assistant</strong></div>
            <button type="button" aria-label="Close project assistant" onClick={() => setOpen(false)}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m6 6 12 12M18 6 6 18"/></svg></button>
          </header>
          <div className="project-chat-widget-messages" aria-live="polite">
            {messages.length === 0 && (
              <div className="project-chat-widget-welcome">
                <p>Ask about project fields, sprints, tasks, deadlines, blockers, priorities, or assignees.</p>
                {suggestions.map((suggestion) => (
                  <button key={suggestion} type="button" disabled={loading} onClick={() => ask(suggestion)}>{suggestion}</button>
                ))}
              </div>
            )}
            {messages.map((message, index) => (
              <div key={`${index}-${message.role}`} className={`project-chat-widget-message project-chat-widget-${message.role}`}>
                <span>{message.role === 'you' ? 'You' : 'Assistant'}</span>
                <p>{message.text}</p>
              </div>
            ))}
            {loading && <p className="project-chat-widget-loading">Checking project data…</p>}
            {error && <p className="message message-error" role="alert">{error}</p>}
          </div>
          <form className="project-chat-widget-form" onSubmit={(event) => { event.preventDefault(); ask(); }}>
            <label className="sr-only" htmlFor="project-chat-widget-question">Ask about a project</label>
            <input
              id="project-chat-widget-question"
              value={question}
              maxLength={1000}
              onChange={(event) => setQuestion(event.target.value)}
              placeholder="Ask a project question…"
              disabled={loading}
            />
            <button className="button button-primary" type="submit" disabled={loading || !question.trim()}>Send</button>
          </form>
        </section>
      )}
      <button
        className="project-chat-launcher"
        type="button"
        aria-expanded={open}
        aria-label={open ? 'Close project assistant' : 'Ask the assistant'}
        title="Ask the assistant"
        onClick={() => setOpen((value) => !value)}
      >
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20 11.5a7.5 7.5 0 0 1-7.5 7.5H6l-3 2v-5.5a7.5 7.5 0 1 1 17-4Z"/><path d="M8 11h.01M12 11h.01M16 11h.01"/></svg>
      </button>
    </div>
  );
}

export default ProjectChatWidget;
