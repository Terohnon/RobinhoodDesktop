using System;
using System.Collections.Generic;
using NumSharp;
using System.Diagnostics;
using System.Text;
using Tensorflow;
using static Tensorflow.Binding;

namespace RobinhoodDesktop.MachineLearning
{
    public class StockML
    {
        #region Variables
        /// <summary>
        /// Should be populated with the input data to train on
        /// </summary>
        public NDArray XInputs = null;

        /// <summary>
        /// Should be populated with the expected outputs for the given inputs
        /// to be used during training
        /// </summary>
        public NDArray YInputs = null;

        /// <summary>
        /// The input layer
        /// </summary>
        protected Tensor Input = null;

        /// <summary>
        /// The labeled examples layer
        /// </summary>
        protected Tensor YTrue = null;

        /// <summary>
        /// Indicates how well the model matched the expected outputs
        /// </summary>
        protected Tensor Accuracy = null;
        
        /// <summary>
        /// The loss function used to train the model
        /// </summary>
        protected Tensor LossFunc = null;

        /// <summary>
        /// The training operation (the chosen optimizer method)
        /// </summary>
        protected Operation TrainOp = null;
        #endregion

        /// <summary>
        /// Default method to build a fully-connected graph of the specified structure
        /// </summary>
        /// <param name="layerNodes">The number of nodes to have at each layer. Index [0] is the number of inputs, and [lenth - 1] is the number of outputs.</param>
        /// <returns></returns>
        public virtual Graph BuildFullyConnectedGraph(int[] layerNodes, float learningRate = 0.01f)
        {
            tf.enable_eager_execution();
            var g = new Graph().as_default();

            Tensor z = null;

            var scope = tf.name_scope("Input");
            tf_with(scope, delegate
            {
                Input = tf.placeholder(tf.float32, shape: new TensorShape(-1, layerNodes[0]));
                YTrue = tf.placeholder(tf.int32, shape: new TensorShape(-1, 1));
            });

            tf_with(tf.variable_scope("FullyConnected"), delegate
            {
                Tensor x = Input;
                Tensor y = null;

                for(int i = 1; i < (layerNodes.Length - 1); i++)
                {
                    var w = tf.get_variable("w", shape: new TensorShape(layerNodes[i - 1], layerNodes[i]), initializer: tf.random_normal_initializer(stddev: 0.1f));
                    var b = tf.get_variable("b", shape: new TensorShape(layerNodes[i]), initializer: tf.constant_initializer(0.1));
                    z = tf.matmul(x, w) + b;
                    y = tf.nn.relu(z);
                    x = y;
                }

                var w2 = tf.get_variable("w2", shape: new TensorShape(layerNodes[layerNodes.Length - 2], layerNodes[layerNodes.Length - 1]), initializer: tf.random_normal_initializer(stddev: 0.1f));
                var b2 = tf.get_variable("b2", shape: new TensorShape(layerNodes[layerNodes.Length - 1]), initializer: tf.constant_initializer(0.1));
                z = tf.matmul(y, w2) + b2;
            });

            tf_with(tf.variable_scope("Loss"), delegate
            {
                var losses = tf.nn.sigmoid_cross_entropy_with_logits(tf.cast(YTrue, tf.float32), z);
                LossFunc = tf.reduce_mean(losses);
            });

            tf_with(tf.variable_scope("Accuracy"), delegate
            {
                var y_pred = tf.cast(z > 0, tf.int32);
                Accuracy = tf.reduce_mean(tf.cast(tf.equal(y_pred, YTrue), tf.float32));
            });

            // We add the training operation, ...
            var adam = tf.train.AdamOptimizer(learningRate);   // Set the learning rate here
            TrainOp = adam.minimize(LossFunc, name: "train_op");

            return g;
        }

        /// <summary>
        /// Prepares the feature and labeled output data to be used to train the model
        /// </summary>
        /// <param name="inputs">The feature input data</param>
        /// <param name="labeledOutputs">The expected outputs corresponding to the inputs</param>
        public virtual void PrepareData(float[,] inputs, int[,] labeledOutputs)
        {
            XInputs = (NDArray)inputs;
            YInputs = (NDArray)labeledOutputs;
        }
        
        /// <summary>
        /// Performs the training operation
        /// </summary>
        public virtual void Train(int epochs = 5000)
        {
            var sw = new Stopwatch();
            sw.Start();

            var config = new ConfigProto
            {
                IntraOpParallelismThreads = 1,
                InterOpParallelismThreads = 1,
                LogDevicePlacement = true
            };

            using(var sess = tf.Session(config))
            {
                // init variables
                sess.run(tf.global_variables_initializer());

                // check the accuracy before training
                var accuracyResult = sess.run(Accuracy, new FeedItem(Input, XInputs), new FeedItem(YTrue, YInputs));
                print($"Pre-accuacy: {accuracyResult}");

                // training
                foreach(var i in range(epochs))
                {
                    // by sampling some input data (fetching)
                    var loss = sess.run(new ValueTuple<ITensorOrOperation, ITensorOrOperation>(TrainOp, LossFunc), new FeedItem(Input, XInputs), new FeedItem(YTrue, YInputs));

                    // We regularly check the loss
                    if(i % 500 == 0)
                        print($"iter:{i} - loss:{loss.Item2}");
                }

                // Finally, we check our final accuracy
                accuracyResult = sess.run(Accuracy, new FeedItem(Input, XInputs), new FeedItem(YTrue, YInputs));
                print($"Post-accuacy: {accuracyResult}");
            }

            print($"Time taken: {sw.Elapsed.TotalSeconds}s");
        }
    }
}
