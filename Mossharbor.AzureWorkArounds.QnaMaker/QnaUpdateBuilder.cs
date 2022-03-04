using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mossharbor.AzureWorkArounds.QnaMaker
{
    using Mossharbor.AzureWorkArounds.QnaMaker.Json;

    /// <summary>
    /// This class builds up the json file that the Update and Replace QnaMaker Rest api's require.
    /// </summary>
    public class QnaUpdateBuilder
    {
        private QnAMaker maker = null;

        /// <summary>
        /// This is the function that modifies the Activity we are trying to build
        /// </summary>
        private Func<UpdateRootobject, UpdateRootobject> fn = null;

        /// <summary>
        /// This function composes the builder function
        /// </summary>
        /// <typeparam name="TA">input type to f1</typeparam>
        /// <typeparam name="TB">output type to f1 and input type to f2</typeparam>
        /// <typeparam name="TC">output type of f2</typeparam>
        /// <param name="f1">first function in the chain to call</param>
        /// <param name="f2">second function in the chain to call</param>
        /// <returns>The function chain</returns>
        private static Func<TA, TC> Compose<TA, TB, TC>(Func<TA, TB> f1, Func<TB, TC> f2)
        {
            return (a) => f2(f1(a));
        }

        /// <summary>
        /// Begin building the QnaMaker Json
        /// </summary>
        /// <returns>A builder to be used in the builder pattern</returns>
        public QnaUpdateBuilder Begin(QnAMaker maker)
        {
            this.maker = maker;
            this.fn = (ignored) => new UpdateRootobject();
            return this;
        }

        private static T[] Append<T>(T[] oldArray, T item)
        {
            if (null == oldArray)
            {
                oldArray = new T[] { item };
                return oldArray;
            }

            T[] newArray = new T[oldArray.Length + 1];
            Array.Copy(oldArray, newArray, oldArray.Length);
            newArray[newArray.Length - 1] = item;
            return newArray;
        }

        private static T[] Concat<T>(T[] oldArray, T[] items)
        {
            T[] newArray = new T[oldArray.Length + items.Length];
            Array.Copy(oldArray, newArray, oldArray.Length);
            Array.Copy(items, 0, newArray, oldArray.Length, items.Length);
            return newArray.Distinct().ToArray();
        }
        
        private Qnalist[] RemoveAnswerIdFrom(Qnalist[] array, int answerId, out bool removed)
        {
            removed = false;
            if (null == array)
                return new Qnalist[0];
            if (!array.Any())
                return array;
            List<Qnalist> qList = new List<Qnalist> (array);

            var enumList = qList.Where(p => p.id == answerId);

            foreach(var t in enumList)
            {
                removed = true;
                qList.Remove(t);
            }

            return qList.ToArray();
        }

        private string[] RemoveFrom(string[] array, string toRemove, out bool removed)
        {
            removed = false;
            if (null == array)
                return new string[0];
            if (!array.Any())
                return array;

            List<string> qList = new List<string>(array);

            if (qList.Contains(toRemove))
            {
                removed = true;
                qList.Remove(toRemove);
            }

            return qList.ToArray();
        }

        /// <summary>
        /// Remove a question from the answer
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="question"></param>
        /// <returns>The builder for the continuation</returns>
        public QnaUpdateBuilder RemoveQuestion(string answer, string question)
        {
            return RemoveQuestions(answer, new string[] { question });
        }

        /// <summary>
        /// removes multiple questions from an answer
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="questions"></param>
        /// <returns>The builder for the continuation</returns>
        public QnaUpdateBuilder RemoveQuestions(string answer, string[] questions)
        {
            this.fn = Compose(this.fn, (updateRootObject) =>
            {
                int answerId = maker.GetAnswerID(answer);
                if (answerId < 0)
                    return updateRootObject;

                if (null == updateRootObject.update)
                {
                    updateRootObject.update = new Update();
                    updateRootObject.update.qnaList = new UpdateQnaList[0];
                }


                UpdateQnaList toUpdate = new UpdateQnaList();
                toUpdate.id = answerId;
                toUpdate.answer = answer;
                toUpdate.questions.delete = questions;
                updateRootObject.update.qnaList = Append(updateRootObject.update.qnaList, toUpdate);

                return updateRootObject;
            });
            return this;
        }

        /// <summary>
        /// Removes all instance of a question in all answers
        /// </summary>
        /// <param name="question"></param>
        /// <returns>The builder for the continuation</returns>
        public QnaUpdateBuilder RemoveQuestion(string question)
        {
            this.fn = Compose(this.fn, (updateRootObject) =>
            {
                maker.ResetCache();
                bool removed = false;
                List<int> answerIdsChecked = new List<int>();
                List<int> answerIdsToDelete = new List<int>();
                foreach (var did in updateRootObject.delete?.ids)
                    answerIdsChecked.Add(did);

                var answerIDs = maker.GetAnswerIDsForQuestion(question);
                foreach (var answerId in answerIDs)
                {
                    // remove it from any already updated  objects
                    foreach (var q in updateRootObject.update?.qnaList)
                    {
                        if (q.id != answerId)
                            continue;

                        q.questions = RemoveFrom(q.questions, question, out removed);

                        if (!q.questions.Any())
                            answerIdsToDelete.Add(q.id);

                        answerIdsChecked.Add(q.id);
                    }

                    // remove it from any already added  objects
                    foreach (var q in updateRootObject.add?.qnaList)
                    {
                        if (q.id != answerId)
                            continue;

                        q.questions = RemoveFrom(q.questions, question, out removed);

                        if (!q.questions.Any())
                            answerIdsToDelete.Add(q.id);
                        answerIdsChecked.Add(q.id);
                    }

                    // remove it from the knowledgebase if we do not already have it.
                    foreach (var q in maker.KBCache)
                    {
                        if (answerIdsChecked.Contains(q.id))
                            continue;

                        if (q.id != answerId)
                            continue;

                        if (null == q.questions)
                            continue;

                        if (Array.BinarySearch<string>(q.questions, question) < 0)
                            continue;

                        if (q.questions.Length == 1)
                        {
                            if (null == updateRootObject.delete)
                                updateRootObject.delete = new Delete();

                            updateRootObject.delete.ids = Append(updateRootObject.delete.ids, q.id);
                        }
                    }
                }

                return updateRootObject;

            });
            return this;
        }

        /// <summary>
        /// Removes an answer
        /// </summary>
        /// <param name="answer"></param>
        /// <returns>The builder for the continuation</returns>
        public QnaUpdateBuilder RemoveAnswer(string answer)
        {
            this.fn = Compose(this.fn, (updateRootObject) =>
            {
                bool removed = false;
                int answerId = maker.GetAnswerID(answer);

                if (updateRootObject.delete?.ids == null || !updateRootObject.delete.ids.Any())
                {
                    if (null == updateRootObject.delete)
                        updateRootObject.delete = new Delete();

                    updateRootObject.delete.ids = new int[1];
                    updateRootObject.delete.ids[0] = answerId;
                    return updateRootObject;
                }
                
                updateRootObject.delete.ids = Append(updateRootObject.delete.ids, answerId);
                updateRootObject.update.qnaList = RemoveAnswerIdFrom(updateRootObject.update.qnaList, answerId,out removed);
                updateRootObject.add.qnaList = RemoveAnswerIdFrom(updateRootObject.add.qnaList, answerId, out removed);
                
                return updateRootObject;

            });
            return this;
        }

        /// <summary>
        /// Adds a question and answer pair.  If the answer already exists in the kb then we append the question if it is new to the answer.
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="question"></param>
        /// <returns>The builder for the continuation</returns>
        public QnaUpdateBuilder AddAnswerToQuestion(string answer, string question)
        {
            return this.AddAnswerToQuestions(answer, new string[] { question });
        }

        /// <summary>
        /// Adds multiple questions to an answer.  If the answer already exists in the kb then we append the questions, if he question is new to the answer.
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="questions"></param>
        /// <returns>The builder for the continuation</returns>
        public QnaUpdateBuilder AddAnswerToQuestions(string answer, string[] questions)
        {
            this.fn = Compose(this.fn, (updateRootObject) =>
            {
                int answerId = maker.GetAnswerID(answer);

                if (answerId >= 0)
                {

                    // we are updating an existing question
                    var answerObj = updateRootObject.update?.qnaList.FirstOrDefault(p => p.id == answerId);
                    if (null != answerObj)
                    {
                        if (null == answerObj.questions)
                            answerObj.questions = new string[0];

                        // this question is already slated for update
                        (answerObj as UpdateQnaList).questions.add = Concat((answerObj as UpdateQnaList).questions.add, questions);
                    }
                    else
                    {
                        // this question is not slated for update yet
                        if (null == updateRootObject.update)
                            updateRootObject.update = new Update();

                        Qnalist answerToUpdate = maker.KBCache.FirstOrDefault(p => p.id == answerId);

                        if (null != answerToUpdate)
                        {
                            UpdateQnaList newAnswer = new UpdateQnaList();
                            newAnswer.id = answerToUpdate.id;
                            newAnswer.answer = answerToUpdate.answer;
                            newAnswer.questions = new UpateQuestions();
                            answerToUpdate = newAnswer;
                            (answerToUpdate as UpdateQnaList).questions.add = questions;
                        }
                        else
                        {
                            UpdateQnaList newAnswer = new UpdateQnaList();
                            newAnswer.id = answerToUpdate.id;
                            newAnswer.answer = answerToUpdate.answer;
                            newAnswer.questions = new UpateQuestions();
                            answerToUpdate = newAnswer;
                            (answerToUpdate as UpdateQnaList).questions.add = Concat((answerToUpdate as UpdateQnaList).questions.add, questions);

                        }
                        updateRootObject.update.qnaList = Append(updateRootObject.update.qnaList, answerToUpdate);
                    }
                }
                else
                {
                    // add new answer and question combo
                    Qnalist item = null;
                    if (null == updateRootObject.add)
                        updateRootObject.add = new Add();

                    if (null == updateRootObject.add?.qnaList)
                    {
                        // we currently dont have any questions added to the update list
                        item = new Qnalist()
                        {
                            answer = answer
                        };

                        (item as Qnalist).questions = questions;
                        updateRootObject.add.qnaList = Append(updateRootObject.add.qnaList, item);
                    }
                    else
                    {
                        var baseQnAList = updateRootObject.add.qnaList.FirstOrDefault(p => p.answer.Equals(answer));

                        if (null == baseQnAList)
                        {
                            // we dont already have this answer so add it to the update list and create the question
                            item = new Qnalist()
                            {
                                answer = answer
                            };

                            (item as Qnalist).questions = questions;
                            updateRootObject.add.qnaList = Append(updateRootObject.add.qnaList, item);
                        }
                        else
                        {
                            // add the question to the existing answer marked for update
                            (baseQnAList as Qnalist).questions = Concat((baseQnAList as Qnalist).questions, questions);
                        }
                    }
                }
                return updateRootObject;

            });
            return this;
        }

        /// <summary>
        /// Run through the function chain and actually build the Json the call the update using the QnaMaker Rest API
        /// </summary>
        /// <returns>true if we got back a successful http response code from the rest api</returns>
        public bool UpdateKnowledgebase()
        {
            var t = this.fn(null);
            bool success = maker.Update(t);
            maker.ResetCache();
            return success;
        }
    }
}
