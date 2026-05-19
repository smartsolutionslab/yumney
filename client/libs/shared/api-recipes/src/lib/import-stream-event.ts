export interface ImportStreamEvent {
  type: 'status' | 'chunk' | 'done' | 'fail';
  data: string;
}
