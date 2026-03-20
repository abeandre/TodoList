export type ISODateTime = string & { __brand: 'ISO' };

export interface ToDo {
  id: string;
  title: string;
  description: string;
  createdAt: ISODateTime;
  updatedAt: ISODateTime;
  finishedAt: ISODateTime | null;
}
